"""Lit un WAV (PCM 8/16/24/32 bits ou flottant) ou un AIFF/AIFF-C (PCM) en tableau numpy (frames, canaux), flottant -1..1."""
import struct

import numpy as np


def _ext80(b):
    # Le nombre à 80 bits de l'AIFF (le taux d'échantillonnage).
    exp = ((b[0] & 0x7F) << 8) | b[1]
    mant = int.from_bytes(b[2:10], 'big')
    if exp == 0 and mant == 0:
        return 0.0
    return mant * 2.0 ** (exp - 16383 - 63)


def _pcm(data, bits, channels, big, fmt=1):
    if fmt == 3:
        dt = np.dtype('>f4' if big else '<f4') if bits == 32 else np.dtype('>f8' if big else '<f8')
        a = np.frombuffer(data, dtype=dt).astype(np.float64)
    elif bits == 8:
        a = np.frombuffer(data, dtype=np.uint8).astype(np.float64)
        a = (a - 128) / 128 if not big else np.frombuffer(data, dtype=np.int8).astype(np.float64) / 128
    elif bits == 16:
        a = np.frombuffer(data, dtype='>i2' if big else '<i2').astype(np.float64) / 32768
    elif bits == 24:
        raw = np.frombuffer(data[:len(data) // 3 * 3], dtype=np.uint8).reshape(-1, 3).astype(np.int32)
        v = (raw[:, 0] << 16 | raw[:, 1] << 8 | raw[:, 2]) if big else (raw[:, 2] << 16 | raw[:, 1] << 8 | raw[:, 0])
        v = np.where(v >= 1 << 23, v - (1 << 24), v)
        a = v.astype(np.float64) / (1 << 23)
    elif bits == 32:
        a = np.frombuffer(data, dtype='>i4' if big else '<i4').astype(np.float64) / 2 ** 31
    else:
        raise ValueError('bits %d' % bits)
    n = len(a) // channels * channels
    return a[:n].reshape(-1, channels)


def read(path):
    d = open(path, 'rb').read()
    if d[:4] == b'RIFF':
        pos, fmt = 12, None
        while pos + 8 <= len(d):
            cid, size = d[pos:pos + 4], struct.unpack('<I', d[pos + 4:pos + 8])[0]
            body = d[pos + 8:pos + 8 + size]
            if cid == b'fmt ':
                tag, ch, rate, _, _, bits = struct.unpack('<HHIIHH', body[:16])
                if tag == 0xFFFE:
                    tag = struct.unpack('<H', body[24:26])[0]
                fmt = (tag, ch, rate, bits)
            elif cid == b'data':
                tag, ch, rate, bits = fmt
                return _pcm(body, bits, ch, False, 3 if tag == 3 else 1), rate
            pos += 8 + size + (size & 1)
    if d[:4] == b'FORM':
        kind = d[8:12]
        pos, comm = 12, None
        while pos + 8 <= len(d):
            cid, size = d[pos:pos + 4], struct.unpack('>I', d[pos + 4:pos + 8])[0]
            body = d[pos + 8:pos + 8 + size]
            if cid == b'COMM':
                ch, frames, bits = struct.unpack('>hIh', body[:8])
                rate = _ext80(body[8:18])
                comp = body[18:22] if kind == b'AIFC' else b'NONE'
                comm = (ch, frames, bits, rate, comp)
            elif cid == b'SSND':
                ch, frames, bits, rate, comp = comm
                offset = struct.unpack('>I', body[:4])[0]
                data = body[8 + offset:]
                if comp in (b'NONE', b'twos'):
                    return _pcm(data, bits, ch, True), int(rate)
                if comp == b'sowt':
                    return _pcm(data, bits, ch, False), int(rate)
                if comp in (b'fl32', b'FL32'):
                    return _pcm(data, 32, ch, True, 3), int(rate)
                raise ValueError('compression %r' % comp)
            pos += 8 + size + (size & 1)
    raise ValueError('format inconnu : ' + path)


def write_wav(path, mono, rate):
    # Mono, 16 bits.
    pcm = (np.clip(mono, -1.0, 1.0) * 32767).astype('<i2').tobytes()
    head = b'RIFF' + struct.pack('<I', 36 + len(pcm)) + b'WAVE' + b'fmt ' + struct.pack('<IHHIIHH', 16, 1, 1, rate, rate * 2, 2, 16)
    open(path, 'wb').write(head + b'data' + struct.pack('<I', len(pcm)) + pcm)
