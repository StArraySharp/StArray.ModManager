/* using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    public Button button;
    public Text text;
    public Dropdown dropdown;
    int c = 0;

    private int lastValue = 0;

    public static NewBehaviourScript instance;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            dropdown.options.Add(new Dropdown.OptionData("option" + i));
        }

        dropdown.value = 5;
        dropdown.onValueChanged.AddListener(delegate(int v)
        {
            if (v != lastValue)
            {
                text.text = "dropdown" + v;
                lastValue = v;
            }
        });
        button.onClick.AddListener(Button_OnClick);
        instance = this;
    }


    void Button_OnClick()
    {
        text.text = "hello" + c;
        c++;
        StartCoroutine(TestCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        c++;
    }

    IEnumerator TestCoroutine()
    {
        Debug.Log("豪爽");
        yield return new WaitForSeconds(1);
        Debug.Log("1000秒后");
    }

    public static void Print()
    {
        Debug.Log("看你吗看");
    }
}
 */