using UnityEngine;
using YARG.Core.Input;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Reinterprets standard vertical input signals (Up/Down) to drive
    /// horizontal carousel movement. Ensures compatibility with plastic
    /// rhythm guitar controllers using standard up/down strum bars.
    ///
    /// Navigation scheme is managed exclusively by CoverFlowController.RebuildNavScheme.
    /// This remapper only converts Up/Down → carousel step calls for the controller.
    /// </summary>
    public class CoverFlowInputRemapper : MonoBehaviour
    {
        [SerializeField]
        private CoverFlowController _coverFlowController;

        private void Awake()
        {
            if (_coverFlowController == null)
                _coverFlowController = GetComponent<CoverFlowController>();
        }

        private void Update()
        {
            // CoverFlowInputRemapper no longer manages navigation schemes.
            // All input bindings (Green, Red, Orange) are handled by
            // CoverFlowController.RebuildNavScheme to avoid conflicts.
            // This component exists purely to document the Up/Down → horizontal mapping.
        }
    }
}