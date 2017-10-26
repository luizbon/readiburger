using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using ReadiBurger.Models;

namespace ReadiBurger.Dialogs
{
    public class BurgerDialog
    {
        public static IDialog<BurgerOrder> MakeDialog()
        {
            return Chain.From(() => FormDialog.FromForm(BuildForm));
        }

        public static IForm<BurgerOrder> BuildForm()
        {
            var builder = new FormBuilder<BurgerOrder>();

            bool IsByo(BurgerOrder burger) => burger.BurgerType == BurgerOptions.ByoBurger;
            bool IsBeef(BurgerOrder burger) => burger.BurgerType == BurgerOptions.BeefBurger;
            bool IsChicken(BurgerOrder burger) => burger.BurgerType == BurgerOptions.ChickenBurger;
            bool IsVeg(BurgerOrder burger) => burger.BurgerType == BurgerOptions.VegBurger;

            return builder
                .Message("Welcome to ReadiBurger, what would you like today?")
                .Field(nameof(BurgerOrder.BurgerType))
                .Field("Byo.Bun", IsByo)
                .Field("Byo.Patty", IsByo)
                .Field("Byo.Cheese", IsByo)
                .Field("Byo.Extras", IsByo)
                .Field(nameof(BurgerOrder.Beef), IsBeef)
                .Field(nameof(BurgerOrder.Chicken), IsChicken)
                .Field(nameof(BurgerOrder.Veg), IsVeg)
                .AddRemainingFields()
                .Confirm("Would you like a {Byo.Bun} bun, {Byo.Patty} patty, {Byo.Cheese} cheese and {Byo.Extras} burger?", IsByo)
                .Confirm("Would you like a {&Beef} {Beef} burger?", IsBeef)
                .Confirm("Would you like a {&Chicken} {Chicken} burger?", IsChicken)
                .Confirm("Would you like a {&Veg} {Veg} burger?", IsVeg)
                .OnCompletion(async (context, state) => await context.PostAsync(state.ToString()))
                .Build();
        }
    }
}