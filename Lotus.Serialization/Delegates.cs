namespace Lotus.Serialization
{
    public delegate TOutput ReadDelegate<out TOutput>();

    public delegate object ReadDelegate();

    public delegate void WriteDelegate<in TInput>(TInput value);

    public delegate void WriteDelegate(object value);
}