using System;
using System.Threading;

namespace AStar.Tools
{
    public class RWLocker : IDisposable
    {
        /// <summary>
        /// Обертка над примитовом синхронизации ReaderWriterLockSlim
        /// https://habr.com/ru/post/459514/#ReaderWriterLockSlim
        /// </summary>
        public struct WriteLockToken : IDisposable
        {
            private readonly ReaderWriterLockSlim @lock;
#if AfterUnlock_Action
            Action _afterUnlock;

            public WriteLockToken(ReaderWriterLockSlim @lock, Action afterUnlock = null) 
#else
            public WriteLockToken(ReaderWriterLockSlim @lock)
#endif
            {
                this.@lock = @lock;
#if AfterUnlock_Action
                _afterUnlock = afterUnlock; 
#endif
                @lock.EnterWriteLock();
            }
            public void Dispose()
            {
                @lock.ExitWriteLock();
#if AfterUnlock_Action
                _afterUnlock?.Invoke(); 
#endif
            }
        }

        public struct ReadLockToken : IDisposable
        {
            private readonly ReaderWriterLockSlim @lock;
#if AfterUnlock_Action
            Action _afterUnlock;

            public ReadLockToken(ReaderWriterLockSlim @lock, Action afterUnlock = null) 
#else
            public ReadLockToken(ReaderWriterLockSlim @lock)
#endif
            {
                this.@lock = @lock;
#if AfterUnlock_Action
                _afterUnlock = afterUnlock; 
#endif
                @lock.EnterReadLock();
            }
            public void Dispose()
            {
                @lock.ExitReadLock();
#if AfterUnlock_Action
                _afterUnlock?.Invoke(); 
#endif
            }
        }

        public struct UpgradableReadToken : IDisposable
        {
            private readonly ReaderWriterLockSlim @lock;
#if AfterUnlock_Action
            Action _afterUnlock;

            public UpgradableReadToken(ReaderWriterLockSlim @lock, Action afterUnlock = null) 
#else
            public UpgradableReadToken(ReaderWriterLockSlim @lock)
#endif
            {
                this.@lock = @lock;
#if AfterUnlock_Action
                _afterUnlock = afterUnlock; 
#endif
                @lock.EnterUpgradeableReadLock();
            }

            public WriteLockToken ReadLock()
            {
                return new WriteLockToken(@lock);
            }

            public void Dispose()
            {
                @lock.ExitUpgradeableReadLock();
#if AfterUnlock_Action
                _afterUnlock?.Invoke(); 
#endif
            }
        }

        private readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

#if AfterUnlock_Action
        public ReadLockToken ReadLock(Action afterUnlock = null) => new ReadLockToken(@lock, afterUnlock);
        public WriteLockToken WriteLock(Action afterUnlock = null) => new WriteLockToken(@lock, afterUnlock);
        public UpgradableReadToken UpgradableReadLock(Action afterUnlock = null) => new UpgradableReadToken(@lock, afterUnlock); 
#else
        public ReadLockToken ReadLock() => new ReadLockToken(@lock);
        public WriteLockToken WriteLock() => new WriteLockToken(@lock);
        public UpgradableReadToken UpgradableReadLock() => new UpgradableReadToken(@lock);
#endif

        public bool IsReadLockHeld => @lock.IsReadLockHeld;
        public bool IsUpgradeableReadLockHeld => @lock.IsUpgradeableReadLockHeld;
        public bool IsWriteLockHeld => @lock.IsWriteLockHeld;

        public void Dispose() => @lock.Dispose();
    }
}
