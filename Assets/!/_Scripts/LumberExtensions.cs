using EMullen.PlayerMgmt;

public static class LumberExtensions 
{
    public static void EnsureLumberData(this PlayerData pd) 
    {
        if(!pd.HasData<GeneralPlayerData>())
            pd.SetData(GeneralPlayerData.CreateDefault(pd.GetUID()));

        if(!pd.HasData<InventoryData>())
            pd.SetData(InventoryData.CreateDefault(pd.GetUID()));
    } 
}