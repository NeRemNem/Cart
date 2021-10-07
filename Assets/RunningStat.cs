using Unity.Mathematics;

public class RunningStat
{
    private int m_n;
    private double m_oldM;
    private double m_newM;
    private double m_oldS;
    private double m_newS;
    public RunningStat()
    {
        m_n = 0;
    }

    public void Clear()
    {
        m_n = 0;
    }

    public void Push(double x)
    {
        m_n++;
        if (m_n == 1)
        {
            m_oldM = m_newM = x;
            m_oldS = 0.0;
        }
        else
        {
            m_newM = m_oldM + (x - m_oldM) / m_n;
            m_newS = m_oldS + (x - m_oldM) * (x - m_newM);

            m_oldM = m_newM;
            m_oldS = m_newS;
        }
    }
    
    public int NumDataValues => m_n;

    public double Mean => m_n > 0 ? m_newM : 0.0;

    public double variance => m_n > 1 ? m_newS / (m_n - 1) : 0.0;
    
    public double Std => math.sqrt(variance);
}

public static class ZFilter
{
    private static RunningStat _running_state = new RunningStat();
    private static float _clip = 1.0f;

    public static double GetScore(double x)
    {
        if (_running_state == null)
            _running_state = new RunningStat();
        _running_state.Push(x);
        x -= _running_state.Mean;
        x /= (_running_state.Std + math.EPSILON);
        x = math.clamp(x, -_clip, _clip);
        return x;
    }
}