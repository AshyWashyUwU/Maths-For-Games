using UnityEngine;

public class CameraSwapper : MonoBehaviour
{
    [SerializeField] private Camera[] cameras;
    private int currentIndex = 0;

    private void Start()
    {
        ActivateCamera(currentIndex);
    }

    public void SwitchCamera()
    {
        cameras[currentIndex].gameObject.SetActive(false);

        currentIndex = (currentIndex + 1) % cameras.Length;

        cameras[currentIndex].gameObject.SetActive(true);
    }

    private void ActivateCamera(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(i == index);
        }
    }
}