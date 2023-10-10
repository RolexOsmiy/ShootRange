using QFSW.MOP2;

public class AutoReturnToPool : PoolableMonoBehaviour
{
    private void OnParticleSystemStopped()
    {
        Release();
    }
}
