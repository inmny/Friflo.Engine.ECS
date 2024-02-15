﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

// ReSharper disable RedundantTypeDeclarationBody
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal class BatchComponent { }

internal class BatchComponent<T> : BatchComponent where T : struct, IComponent
{
    internal        T       value;
}


internal sealed class  EntityBatch
{
    public   override   string              ToString() => GetString();

    #region internal fields
    internal readonly   BatchComponent[]    components;         //  8
    private  readonly   EntityStoreBase     store;              //  8
    internal readonly   EntityStore         entityStore;        //  8
    internal            int                 entityId;           //  4
    internal            Tags                addTags;            // 32
    internal            Tags                removeTags;         // 32
    internal            ComponentTypes      addComponents;      // 32
    internal            ComponentTypes      removeComponents;   // 32
    #endregion
    
#region internal methods
    internal EntityBatch(EntityStoreBase store)
    {
        this.store          = store;
        entityStore         = (EntityStore)store;
        var schema          = EntityStoreBase.Static.EntitySchema;
        int maxStructIndex  = schema.maxStructIndex;
        components          = new BatchComponent[maxStructIndex];
        
        var componentTypes = schema.components;
        for (int n = 1; n < maxStructIndex; n++) {
            components[n] = componentTypes[n].CreateBatchComponent();
        }
    }
    
    private string GetString()
    {
        var hasAdds     = addComponents.Count    > 0 || addTags.Count    > 0;
        var hasRemoves  = removeComponents.Count > 0 || removeTags.Count > 0;
        if (!hasAdds && !hasRemoves) {
            return "empty";
        }
        var sb = new StringBuilder();
        if (hasAdds) {
            sb.Append("add: [");
            foreach (var component in addComponents) {
                sb.Append(component.Name);
                sb.Append(", ");
            }
            foreach (var tag in addTags) {
                sb.Append('#');
                sb.Append(tag.Name);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append("]  ");
        }
        if (hasRemoves) {
            sb.Append("remove: [");
            foreach (var component in removeComponents) {
                sb.Append(component.Name);
                sb.Append(", ");
            }
            foreach (var tag in removeTags) {
                sb.Append('#');
                sb.Append(tag.Name);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append(']');
        }
        return sb.ToString();
    }
    
    private void Clear()
    {
        addTags             = default;
        removeTags          = default;
        addComponents       = default;
        removeComponents    = default;
    }
    
    #endregion
    
#region commands
    internal void Apply() {
        try {
            store.ApplyEntityBatch(this);
        }
        finally {
            Clear();
        }
    }
    
    internal EntityBatch AddComponent<T>(T component) where T : struct, IComponent
    {
        var structIndex = StructHeap<T>.StructIndex;
        addComponents.      bitSet.SetBit   (structIndex);
        removeComponents.   bitSet.ClearBit (structIndex);
        ((BatchComponent<T>)components[structIndex]).value = component;
        return this;   
    }
    
    internal EntityBatch RemoveComponent<T>() where T : struct, IComponent
    {
        var structIndex = StructHeap<T>.StructIndex;
        removeComponents.   bitSet.SetBit   (structIndex);
        addComponents.      bitSet.ClearBit (structIndex);
        return this;   
    }
    
    internal EntityBatch AddTag<T>() where T : struct, ITag
    {
        var tagIndex = TagType<T>.TagIndex;
        addTags.    bitSet.SetBit   (tagIndex);
        removeTags. bitSet.ClearBit (tagIndex);
        return this;
    }
    
    internal EntityBatch AddTags(in Tags tags)
    {
        addTags.    Add     (tags);
        removeTags. Remove  (tags);
        return this;
    }
    
    internal EntityBatch RemoveTag<T>() where T : struct, ITag
    {
        var tagIndex = TagType<T>.TagIndex;
        removeTags. bitSet.SetBit   (tagIndex);
        addTags.    bitSet.ClearBit (tagIndex);
        return this;
    }
    
    internal EntityBatch RemoveTags(in Tags tags)
    {
        addTags.    Remove  (tags);
        removeTags. Add     (tags);
        return this;
    }
    #endregion
}
