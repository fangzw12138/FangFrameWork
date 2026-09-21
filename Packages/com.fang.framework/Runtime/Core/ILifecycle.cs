namespace Fang.Framework
{
    public interface ILifecycle
    {
        void OnInit();

        void OnDispose();
    }

    public interface ITickable
    {
        void OnTick(float deltaTime);
    }

    public interface IFixedTickable
    {
        void OnFixedTick(float deltaTime);
    }
}
