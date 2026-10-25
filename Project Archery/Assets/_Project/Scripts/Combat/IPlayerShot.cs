namespace Archery.Combat
{
    /// <summary>
    /// Un tir du joueur qui n'est pas une flèche (une faille du katana, 24 octobre), donné comme source de ses dégâts : il
    /// compte comme une flèche pour le score (points de touche, combo) et le vampirisme ; un tir « en plus » (multitir,
    /// écho, déluge) ne rapporte pas de points de touche, comme une flèche en plus.
    /// </summary>
    public interface IPlayerShot
    {
        /// <summary>Un tir en plus (multitir, écho, déluge) : ni points de touche ni combo, mais ses éliminations comptent.</summary>
        bool IsExtra { get; }
    }
}
