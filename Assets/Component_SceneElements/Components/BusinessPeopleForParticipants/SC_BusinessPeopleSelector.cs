using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SC_BusinessPeopleSelector : NetworkBehaviour
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum FemaleSuitType
    {
        Skirt,
        Trouser
    }

    [SerializeField] private NetworkVariable<int> seed = new NetworkVariable<int>();

    [SerializeField] private Gender gender;
    [SerializeField] private FemaleSuitType femaleSuitType;

    public List<GameObject> MaleHeads;
    public List<GameObject> MaleSuits;
    public List<GameObject> FemaleSkirtHeads;
    public List<GameObject> FemaleSkirts;
    public List<GameObject> FemaleTrouserHeads;
    public List<GameObject> FemaleTrousers;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            seed.Value = Random.Range(0, int.MaxValue);

            InitializeRandomState(seed.Value);
        }
    }

    private IEnumerator Start()
    {
        if (IsClient && !IsServer)
        {
            yield return new WaitUntil(() => seed.Value != 0);
            InitializeRandomState(seed.Value);
        }
    }


    private void InitializeRandomState(int seed)
    {
        Random.InitState(seed);
        SetAppearance();
    }



    public void SetAppearance()
    {
        gender = GetRandomEnumValue<Gender>();

        if (gender == Gender.Male)
        {
            Debug.Log("Gender: male");
            SelectAndDestroyRest(MaleHeads);
            SelectAndDestroyRest(MaleSuits);
            DestroyAll(FemaleSkirts);
            DestroyAll(FemaleSkirtHeads);
            DestroyAll(FemaleTrousers);
            DestroyAll(FemaleTrouserHeads);
        }
        else if (gender == Gender.Female)
        {
            Debug.Log("Gender: female");
            femaleSuitType = GetRandomEnumValue<FemaleSuitType>();

            if (femaleSuitType == FemaleSuitType.Skirt)
            {
                SelectAndDestroyRest(FemaleSkirts);
                SelectAndDestroyRest(FemaleSkirtHeads);
                DestroyAll(FemaleTrousers);
                DestroyAll(FemaleTrouserHeads);
            }
            else
            {
                SelectAndDestroyRest(FemaleTrousers);
                SelectAndDestroyRest(FemaleTrouserHeads);
                DestroyAll(FemaleSkirts);
                DestroyAll(FemaleSkirtHeads);
            }

            DestroyAll(MaleHeads);
            DestroyAll(MaleSuits);
        }
    }

    T GetRandomEnumValue<T>()
    {
        T[] values = (T[])System.Enum.GetValues(typeof(T));
        int randomIndex = Random.Range(0, values.Length);
        return values[randomIndex];
    }

    void SelectAndDestroyRest(List<GameObject> list)
    {
        if (list.Count == 0) return;

        GameObject selected = list[Random.Range(0, list.Count)];
        foreach (var item in list)
        {
            if (item != selected)
                Destroy(item);
        }
        list.Clear();
        list.Add(selected);
    }

    void DestroyAll(List<GameObject> list)
    {
        foreach (var item in list)
        {
            Destroy(item);
        }
        list.Clear();
    }
}
