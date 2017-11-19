using System.Collections;

namespace Sheduler
{
    public abstract class Task
    {
        abstract public IEnumerator Run();
    }
}
