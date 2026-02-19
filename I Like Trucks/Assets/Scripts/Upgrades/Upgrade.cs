using UnityEngine;

[CreateAssetMenu(fileName = "BasicUpgrade", menuName = "Upgrade/BasicUpgrade")]
public class Upgrade : BaseUpgrade
{
    public override void OnCreate()
    {
        //Code here executes when the upgrade initially appears in selection
    }
    public override void OnApply()
    {
        //Code here executes if/when the upgrade is added to active modifiers

        base.OnApply();

        //Insert extra code here that does more than just modify a stat (i.e change a model, rb info, custom script to player)
    }

    public override void OnRemove()
    {
        //Code here executes if/when the upgrade is removed from active modifiers
    }
}
