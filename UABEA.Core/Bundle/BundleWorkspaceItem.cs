using System.IO;

namespace UABEA.Core.Bundle
{
    public class BundleWorkspaceItem
    {
        public string Name { get; set; }

        public string OriginalName { get; }

        public bool IsNew { get; }

        public bool IsSerialized { get; }

        public bool IsRemoved { get; set; }

        public bool IsModified { get; }

        public Stream Stream { get; }

        public BundleWorkspaceItem(
            string name,
            string originalName,
            bool isNew,
            bool isSerialized,
            bool isModified,
            Stream stream)
        {
            Name = name;
            OriginalName = originalName;
            IsNew = isNew;
            IsSerialized = isSerialized;
            IsModified = isModified;
            Stream = stream;

            IsRemoved = false;
        }

        public override string ToString()
        {
            return Name + (IsModified ? "*" : "");
        }
    }
}
