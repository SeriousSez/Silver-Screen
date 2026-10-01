using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverScreen.Domain.Characters
{
    // These contracts intentionally do not refer to a Unity mesh, renderer or prefab.
    public enum WardrobeSlot
    {
        Headwear, Shirt, Waistcoat, Jacket, Outerwear, Trousers, Dress, Skirt,
        Footwear, Neckwear, Glasses, EarJewelry, NeckJewelry, LeftWrist,
        RightWrist, LeftHand, RightHand, WaistAccessory, OtherAccessory
    }

    public enum AppearanceSocket { Headwear, Eyes, LeftEar, RightEar, Neck, LeftWrist, RightWrist, LeftHand, RightHand, Waist }

    [Serializable]
    public sealed class PhysicalMeasurements
    {
        public float barefootHeightMetres, eyeHeightMetres, shoulderWidthMetres, footLengthMetres;
        public PhysicalMeasurements Copy() => (PhysicalMeasurements)MemberwiseClone();
    }

    [Serializable]
    public sealed class VisualIdentity
    {
        public const int CurrentSchema = 1;
        public int schemaVersion = CurrentSchema;
        public string personId, foundationId, faceIdentityId, rigVersion;
        public PhysicalMeasurements measurements = new PhysicalMeasurements();
        public float[] anatomicalControls = Array.Empty<float>();

        public VisualIdentity Copy()
        {
            var copy=(VisualIdentity)MemberwiseClone();
            copy.measurements=measurements?.Copy();copy.anatomicalControls=(float[])anatomicalControls.Clone();return copy;
        }

        public void Validate()
        {
            if(schemaVersion!=CurrentSchema||string.IsNullOrWhiteSpace(personId)||string.IsNullOrWhiteSpace(foundationId)||
               string.IsNullOrWhiteSpace(faceIdentityId)||string.IsNullOrWhiteSpace(rigVersion)||measurements==null||anatomicalControls==null)
                throw new ArgumentException("Identity requires versioned anatomy, face, rig and physical measurements.");
            if(!Finite(measurements.barefootHeightMetres)||measurements.barefootHeightMetres<=0||
               !Finite(measurements.eyeHeightMetres)||measurements.eyeHeightMetres<=0||measurements.eyeHeightMetres>=measurements.barefootHeightMetres||
               !Finite(measurements.shoulderWidthMetres)||measurements.shoulderWidthMetres<=0||
               !Finite(measurements.footLengthMetres)||measurements.footLengthMetres<=0||anatomicalControls.Any(v=>!Finite(v)))
                throw new ArgumentException("Invalid anatomical measurements or controls.");
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }

    [Serializable]
    public sealed class WardrobeItem
    {
        public WardrobeSlot slot;
        // Empty means explicitly remove the slot. A missing entry means inherit.
        public string assetId;
        public WardrobeItem Copy() => (WardrobeItem)MemberwiseClone();
    }

    [Serializable]
    public sealed class OutfitSelection
    {
        public WardrobeItem[] items=Array.Empty<WardrobeItem>();
        public OutfitSelection Copy()=>new OutfitSelection{items=items.Select(x=>x.Copy()).ToArray()};
        public string Get(WardrobeSlot slot)=>items.FirstOrDefault(x=>x.slot==slot)?.assetId??string.Empty;
        public void Validate()
        {
            if(items==null||items.Any(x=>x==null||x.assetId==null||!Enum.IsDefined(typeof(WardrobeSlot),x.slot))||items.GroupBy(x=>x.slot).Any(g=>g.Count()!=1))
                throw new ArgumentException("Each wardrobe slot may occur once; empty IDs explicitly remove an item.");
        }
        public OutfitSelection Overlay(OutfitSelection changes)
        {
            Validate();changes?.Validate();
            var result=items.ToDictionary(x=>x.slot,x=>x.Copy());
            if(changes!=null)foreach(var item in changes.items)result[item.slot]=item.Copy();
            return new OutfitSelection{items=result.Values.OrderBy(x=>x.slot).ToArray()};
        }
    }

    [Serializable]
    public sealed class PersonalStyle
    {
        public string hairstyleId=string.Empty, facialHairId=string.Empty, eyebrowsId=string.Empty;
        public string hairColor="#382B23";
        public float greyFraction;
        public OutfitSelection outfit=new OutfitSelection();
        public PersonalStyle Copy()
        {
            var copy=(PersonalStyle)MemberwiseClone();copy.outfit=outfit?.Copy();return copy;
        }
        public void Validate()
        {
            if(hairstyleId==null||facialHairId==null||eyebrowsId==null||string.IsNullOrWhiteSpace(hairColor)||outfit==null||
               float.IsNaN(greyFraction)||greyFraction<0||greyFraction>1)throw new ArgumentException("Invalid personal styling.");
            outfit.Validate();
        }
    }

    [Serializable]
    public sealed class AppearanceState
    {
        public VisualIdentity identity;
        public PersonalStyle personalStyle=new PersonalStyle();
        public int revision;
        public void Validate(){if(identity==null||personalStyle==null||revision<0)throw new ArgumentException("Incomplete appearance state.");identity.Validate();personalStyle.Validate();}
    }

    [Serializable]
    public sealed class ProductionAppearance
    {
        public string productionId, personId;
        public OutfitSelection costumeOverrides=new OutfitSelection();
        // Null inherits; empty removes. No production field can override anatomy.
        public string hairstyleOverride, facialHairOverride, eyebrowsOverride, hairColorOverride;
        // Unity cannot serialize Nullable<T>. Keep optional-value semantics at the
        // API boundary and store the presence/value explicitly like the other DTO data.
        public bool hasGreyFractionOverride;
        public float greyFractionOverrideValue;
        public float? greyFractionOverride
        {
            get => hasGreyFractionOverride ? greyFractionOverrideValue : (float?)null;
            set { hasGreyFractionOverride = value.HasValue; greyFractionOverrideValue = value.GetValueOrDefault(); }
        }
    }

    /// <summary>Frozen appearance for a movie/performance. Accessors return copies.</summary>
    public sealed class AppearanceSnapshot
    {
        private readonly VisualIdentity identity;
        private readonly PersonalStyle style;
        public string ProductionId { get; }
        public int SourceRevision { get; }
        public VisualIdentity Identity=>identity.Copy();
        public PersonalStyle Style=>style.Copy();
        internal AppearanceSnapshot(VisualIdentity identity,PersonalStyle style,string production,int revision)
        {this.identity=identity.Copy();this.style=style.Copy();ProductionId=production;SourceRevision=revision;}
    }

    public static class AppearanceResolver
    {
        public static AppearanceSnapshot Resolve(AppearanceState person,ProductionAppearance production=null)
        {
            if(person==null)throw new ArgumentNullException(nameof(person));person.Validate();
            var style=person.personalStyle.Copy();
            if(production!=null)
            {
                if(string.IsNullOrWhiteSpace(production.productionId)||production.personId!=person.identity.personId)
                    throw new ArgumentException("Production appearance must target this person and a stable production ID.");
                style.outfit=style.outfit.Overlay(production.costumeOverrides);
                style.hairstyleId=production.hairstyleOverride??style.hairstyleId;
                style.facialHairId=production.facialHairOverride??style.facialHairId;
                style.eyebrowsId=production.eyebrowsOverride??style.eyebrowsId;
                style.hairColor=production.hairColorOverride??style.hairColor;
                style.greyFraction=production.greyFractionOverride??style.greyFraction;
            }
            style.Validate();
            return new AppearanceSnapshot(person.identity,style,production?.productionId,person.revision);
        }
    }
}
