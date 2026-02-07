using Engine.Assets;

namespace Engine.Editor
{
    public class EditorEmbeddedAssets : EmbeddedAssetDatabase
    {
        public static readonly AssetID ArrowPointerID = "Internal:ArrowModel";

        public EditorEmbeddedAssets()
        {
            RegisterAsset(ArrowPointerID, ArrowPointerModel.MeshData);
        }
    }
}
