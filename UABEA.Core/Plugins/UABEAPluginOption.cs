using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.Threading.Tasks;
using UABEA.Core.Assets;

namespace UABEA.Core.Plugins
{
    public interface UABEAPluginOption
    {
        bool SelectionValidForPlugin(
            AssetsManager am,
            UABEAPluginAction action,
            List<AssetContainer> selection,
            out string name);

        Task<bool> ExecutePlugin(
            object? context,
            AssetWorkspace workspace,
            List<AssetContainer> selection);
    }
}
