using Engine.Core.Scenes.Loader.Info;

namespace Engine.Core.Scenes.Loader;

public interface ISceneCreator
{
    Scene Create(SceneInfo info);
}