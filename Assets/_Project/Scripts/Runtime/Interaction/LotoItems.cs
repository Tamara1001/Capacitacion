using LotoSim.Core;

namespace LotoSim.Interaction
{
    /// <summary>Nombres visibles de los dispositivos LOTO.</summary>
    public static class LotoItems
    {
        public static string Name(LotoItemType type) => type switch
        {
            LotoItemType.Padlock => "Candado",
            LotoItemType.Tag => "Etiqueta",
            LotoItemType.ValveLockout => "Bloqueo de válvula",
            _ => "Nada"
        };

        public static string WithArticle(LotoItemType type) => type switch
        {
            LotoItemType.Padlock => "el candado",
            LotoItemType.Tag => "la etiqueta",
            LotoItemType.ValveLockout => "el bloqueo de válvula",
            _ => "nada"
        };
    }
}
