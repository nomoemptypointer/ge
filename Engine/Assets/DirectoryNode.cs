namespace Engine.Assets
{
    public class AssetInfo(string name, string path)
    {
        public string Name { get; } = name;
        public string Path { get; } = path;
    }

    public class DirectoryNode(string path, AssetInfo[] assetInfos, DirectoryNode[] children)
    {
        public string FullPath { get; } = path;
        public string FolderName { get; set; } = new DirectoryInfo(path).Name;
        public AssetInfo[] AssetInfos { get; } = assetInfos;
        public DirectoryNode[] Children { get; } = children;
    }
}
