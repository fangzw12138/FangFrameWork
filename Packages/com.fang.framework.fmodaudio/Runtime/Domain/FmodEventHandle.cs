namespace Fang.Framework.FmodAudio
{
    public readonly struct FmodEventHandle
    {
        public static readonly FmodEventHandle Invalid = new FmodEventHandle(0, 0);

        public readonly int InstanceId;

        public readonly int ServiceGeneration;

        public bool IsValid => InstanceId > 0;

        internal FmodEventHandle(int instanceId, int serviceGeneration)
        {
            InstanceId = instanceId;
            ServiceGeneration = serviceGeneration;
        }
    }
}
