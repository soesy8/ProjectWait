using System.Collections;
using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>Hides a scene intro card after it has been visible briefly.</summary>
    public sealed class OfficeBlackoutTimedCard : MonoBehaviour
    {
        [SerializeField, Min(0f)] float visibleDuration = 3f;

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(visibleDuration);
            gameObject.SetActive(false);
        }
    }
}
