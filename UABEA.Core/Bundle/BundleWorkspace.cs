using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace UABEA.Core.Bundle
{
    public class BundleWorkspace
    {
        public BundleFileInstance? BundleInst { get; set; }

        public AssetsManager am { get; }

        public ObservableCollection<BundleWorkspaceItem> Files { get; }

        public Dictionary<string, BundleWorkspaceItem> FileLookup { get; }

        public HashSet<string> RemovedFiles { get; }

        public BundleWorkspace()
        {
            BundleInst = null;
            am = new AssetsManager();

            Files = new ObservableCollection<BundleWorkspaceItem>();
            FileLookup = new Dictionary<string, BundleWorkspaceItem>();
            RemovedFiles = new HashSet<string>();
        }

        public void Reset(BundleFileInstance? bundleInst)
        {
            BundleInst = bundleInst;

            Files.Clear();
            FileLookup.Clear();
            RemovedFiles.Clear();

            if (bundleInst != null)
                PopulateFilesList();
        }

        private void PopulateFilesList()
        {
            var dirInfs = BundleInst.file.BlockAndDirInfo.DirectoryInfos;

            foreach (var dirInf in dirInfs)
            {
                string name = dirInf.Name;
                long startAddress = dirInf.Offset;
                long length = dirInf.DecompressedSize;

                SegmentStream stream = new SegmentStream(
                    BundleInst.file.DataReader.BaseStream,
                    startAddress,
                    length);

                BundleWorkspaceItem wsItem =
                    new BundleWorkspaceItem(
                        name,
                        name,
                        false,
                        (dirInf.Flags & 0x04) != 0,
                        false,
                        stream);

                Files.Add(wsItem);
                FileLookup[name] = wsItem;
            }
        }

        public void AddOrReplaceFile(Stream stream, string name, bool isSerialized, string? prevName = null)
        {
            if (prevName == null)
                prevName = name;

            if (FileLookup.ContainsKey(prevName))
            {
                BundleWorkspaceItem wsItem;

                int fileListIndex = Files.IndexOf(FileLookup[prevName]);

                if (fileListIndex != -1)
                {
                    wsItem = new BundleWorkspaceItem(
                        name,
                        Files[fileListIndex].OriginalName,
                        false,
                        isSerialized,
                        true,
                        stream);

                    Files[fileListIndex] = wsItem;
                }
                else
                {
                    wsItem = new BundleWorkspaceItem(
                        name,
                        prevName,
                        false,
                        isSerialized,
                        true,
                        stream);
                }

                if (FileLookup[prevName].IsNew)
                    FileLookup[prevName].Stream.Close();

                FileLookup.Remove(prevName);
                FileLookup[name] = wsItem;
            }
            else
            {
                BundleWorkspaceItem wsItem =
                    new BundleWorkspaceItem(
                        name,
                        name,
                        false,
                        isSerialized,
                        true,
                        stream);

                Files.Add(wsItem);
                FileLookup[name] = wsItem;
            }
        }

        public void RenameFile(string origName, string newName)
        {
            if (FileLookup.ContainsKey(origName))
            {
                BundleWorkspaceItem item = FileLookup[origName];

                item.Name = newName;

                FileLookup.Remove(origName);
                FileLookup[newName] = item;
            }
        }

        public List<BundleReplacer> GetReplacers()
        {
            List<BundleReplacer> replacers = new();

            foreach (string name in RemovedFiles)
            {
                replacers.Add(new BundleRemover(name));
            }

            foreach (BundleWorkspaceItem item in FileLookup.Values)
            {
                if (!item.IsRemoved)
                {
                    if (item.IsModified)
                    {
                        replacers.Add(
                            new BundleReplacerFromStream(
                                item.OriginalName,
                                item.Name,
                                item.IsSerialized,
                                item.Stream,
                                0,
                                -1));
                    }
                    else if (item.Name != item.OriginalName)
                    {
                        replacers.Add(
                            new BundleRenamer(
                                item.OriginalName,
                                item.Name));
                    }
                }
            }

            return replacers;
        }
    }
}
