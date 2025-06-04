using Engine.Core.Scenes.Loader.Info;

namespace Engine.Core.Scenes.Loader;

public interface ISceneLoader
{
    SceneInfo Load(string path);
}