namespace VideoUploader.Services.MessageBus;

public interface IMessageBus
{
    void Publish(string queue, byte[] message);
}