// Substitutos, só para a checagem de compilação, dos tipos de Runtime/InternalBridge do UGUI 2.x mais novo
// (dependem de APIs internas do motor e não compilam fora do Unity).
namespace TMPro
{
    internal class TMPHelpURL : System.Attribute
    {
        internal TMPHelpURL(string pageName) { }
    }

    public static class ObjectUtilsBridge
    {
        public static void MarkDirty(this UnityEngine.Object obj) { }
    }
}
