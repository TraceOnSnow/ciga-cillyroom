using UnityEngine;
using UnityEngine.EventSystems;

namespace Subspace
{
    public sealed class SubspaceButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private SubspaceAudioController audioController;
        [SerializeField] private bool playHoverSound = true;
        [SerializeField] private bool playConfirmSound = true;

        public void Configure(SubspaceAudioController controller)
        {
            audioController = controller;
        }

        public void SetPlayback(bool hover, bool confirm)
        {
            playHoverSound = hover;
            playConfirmSound = confirm;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playHoverSound)
            {
                Audio?.PlayHover();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playConfirmSound)
            {
                Audio?.PlayConfirm();
            }
        }

        private SubspaceAudioController Audio
        {
            get
            {
                if (audioController == null)
                {
                    audioController = SubspaceAudioController.Instance;
                }

                return audioController;
            }
        }
    }
}
