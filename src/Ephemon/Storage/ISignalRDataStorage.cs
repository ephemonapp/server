using Ephemon.Data;
using Ephemon.Storage.Infrastructure;

namespace Ephemon.Storage;

public interface ISignalRDataStorage : IStorage<string, SignalRData>;