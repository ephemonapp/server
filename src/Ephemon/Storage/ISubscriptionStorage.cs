using Ephemon.Data;
using Ephemon.Storage.Infrastructure;

namespace Ephemon.Storage;

public interface ISubscriptionStorage : IStorage<string, Subscription>;