using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Bot.Builder.FormFlow;

namespace ReadiBurger.Models
{
    [Serializable]
    public class BurgerOrder
    {
        public BurgerOptions BurgerType { get; set; }
        public BeefOptions Beef { get; set; }
        public ChickenOptions Chicken { get; set; }
        public VegOptions Veg { get; set; }
        public ByoBurger Byo { get; set; }
        [Optional]
        public CouponOptions Coupon;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("BurgerOrder(");
            switch (BurgerType)
            {
                case BurgerOptions.ByoBurger:
                    builder.AppendFormat("{0}, {1}, {2}, {3}, [", BurgerType, Byo.Bun, Byo.Patty, Byo.Cheese);
                    foreach (var extra in Byo.Extras)
                    {
                        builder.AppendFormat("{0} ", extra);
                    }
                    builder.AppendFormat("]");
                    break;
                case BurgerOptions.BeefBurger:
                    builder.AppendFormat("{0}, {1}", BurgerType, Beef);
                    break;
                case BurgerOptions.ChickenBurger:
                    builder.AppendFormat("{0}, {1}", BurgerType, Chicken);
                    break;
                case BurgerOptions.VegBurger:
                    builder.AppendFormat("{0}, {1}", BurgerType, Veg);
                    break;
            }
            if(IsValidBeefCoupon() || IsValidChickenCoupon())
                builder.AppendFormat(", {0}", Coupon);
            builder.Append(")");
            return builder.ToString();
        }

        private bool IsValidChickenCoupon()
        {
            return Coupon == CouponOptions.Chicken20Percent && (BurgerType == BurgerOptions.ChickenBurger || Byo?.Patty == PattyOptions.Chichen);
        }

        private bool IsValidBeefCoupon()
        {
            return Coupon == CouponOptions.Beef20Percent && (BurgerType == BurgerOptions.BeefBurger || Byo?.Patty == PattyOptions.Beef);
        }
    }

    [Serializable]
    public class ByoBurger
    {
        public BunOptions Bun { get; set; }
        public PattyOptions Patty { get; set; }
        public CheeseOptions Cheese { get; set; }
        public List<ExtraOptions> Extras { get; set; } = new List<ExtraOptions>();
    }
    public enum CouponOptions { Beef20Percent = 1, Chicken20Percent };

    public enum VegOptions
    {
        Soy = 1
    }

    public enum ChickenOptions
    {
        Crispy = 1,
        Grilled
    }

    public enum BeefOptions
    {
        Classic = 1,
        Double
    }

    public enum BunOptions
    {
        Classic = 1,
        [Terms("gluten free", "GF")]
        [Describe("Gluten Free")]
        Gluten,
        Brioche
    }

    public enum PattyOptions
    {
        Beef = 1,
        Chichen,
        Pork,
        Soy
    }

    public enum CheeseOptions
    {
        American = 1,
        Cheddar,
        Tasty
    }

    public enum ExtraOptions
    {
        [Terms("tomato sauce", "ketchup")]
        Ketchup = 1,
        Mustard,
        Pickles,
        Bacon,
        Tomato,
        Lettuce,
        Onion
    }

    public enum BurgerOptions
    {
        BeefBurger = 1, ChickenBurger, VegBurger,

        [Terms("byo", "build your own")]
        [Describe("Build your own")]
        ByoBurger
    }
}