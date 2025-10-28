using UnityEngine;

public class EfectoMoho : MonoBehaviour
{
    public Player2 Player;
    public GameObject Efectomoho;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Player.infectado2 == true)
        {
            Efectomoho.gameObject.SetActive(true);
        }
        else
        {
            Efectomoho.gameObject.SetActive(false);

        }
    }     
}
