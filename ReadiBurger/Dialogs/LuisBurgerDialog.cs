using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using ReadiBurger.Models;

namespace ReadiBurger.Dialogs
{
    [LuisModel("fc6ca6f7-9e20-4271-8290-216c4d432c80", "2c5ebc19e8c2472c8f6d9c675d9945f2")]
    [Serializable]
    public class LuisBurgerDialog : LuisDialog<BurgerOrder>
    {
        private readonly BuildFormDelegate<BurgerOrder> _makeBurgerForm;

        public LuisBurgerDialog(BuildFormDelegate<BurgerOrder> makeBurgerForm)
        {
            _makeBurgerForm = makeBurgerForm;
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I'm sorry. I didn't understand you.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("OrderBurger")]
        [LuisIntent("UseCoupon")]
        public async Task ProcessBurgerOrder(IDialogContext context, LuisResult result)
        {
            var entities = new List<EntityRecommendation>(result.Entities);

            if (entities.All(entity => entity.Type != "BurgerType"))
            {
                foreach (var entity in result.Entities)
                {
                    string type = null;
                    switch (entity.Type)
                    {
                        case "Beef": type = "Beef"; break;
                        case "Chicken": type = "Chicken"; break;
                        case "Veg": type = "Veg"; break;
                        default:
                            if (entity.Type.StartsWith("BYO", StringComparison.InvariantCultureIgnoreCase)) type = "byo";
                            break;
                    }

                    if (type == null) continue;

                    entities.Add(new EntityRecommendation(type: "BurgerType") { Entity = type });
                    break;
                }
            }

            var burgerForm = new FormDialog<BurgerOrder>(new BurgerOrder(), _makeBurgerForm, FormOptions.PromptInStart, entities);
            context.Call(burgerForm, BurgerFormComplete);
        }

        private async Task BurgerFormComplete(IDialogContext context, IAwaitable<BurgerOrder> result)
        {
            BurgerOrder order = null;
            try
            {
                order = await result;
            }
            catch (OperationCanceledException e)
            {
                await context.PostAsync("You canceled the form!");
                return;
            }

            if (order != null)
            {
                await context.PostAsync("Your Burger Order: " + order.ToString());
            }
            else
            {
                await context.PostAsync("Form returned empty response!");
            }

            context.Wait(MessageReceived);
        }
    }
}