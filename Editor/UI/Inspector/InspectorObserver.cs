﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Friflo.Fliox.Engine.ECS;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
namespace Friflo.Fliox.Editor.UI.Inspector;


internal readonly struct ComponentItem
{
    internal readonly   InspectorComponent  control;
    internal readonly   Panel               panel;
    internal readonly   ComponentField[]    fields;
    
    internal ComponentItem(InspectorComponent control, Panel panel, ComponentField[] fields) {
        this.control    = control;
        this.panel      = panel;
        this.fields     = fields;
    }
} 


internal class InspectorObserver : EditorObserver
{
    private readonly    InspectorControl                            inspector;
    private readonly    Dictionary<ComponentType, ComponentItem>    componentMap;
    private readonly    Dictionary<Type,          ComponentItem>    scriptMap;
    
    
    internal InspectorObserver (InspectorControl inspector, Editor editor) : base (editor)
    {
        this.inspector  = inspector;
        componentMap    = new Dictionary<ComponentType, ComponentItem>();
        scriptMap       = new Dictionary<Type,          ComponentItem>();
    }

    protected override void OnSelectionChanged(in EditorSelection selection)
    {
        var item    = selection.item;
        var entity  = item?.Entity;
        if (entity != null) {
            var archetype           = entity.Archetype;
            var model               = inspector.model;
            model.TagCount          = archetype.Tags.Count;
            model.ComponentCount    = archetype.Structs.Count;
            model.ScriptCount       = entity.Scripts.Length;
            AddEntityControls(entity);
        }
    }
    
    private void AddEntityControls(Entity entity)
    {
        // Console.WriteLine($"--- Inspector entity: {entity}");
        var tags        = inspector.Tags.Children;
        tags.Clear();
        var archetype = entity.Archetype;
        
        // --- tags
        foreach (var tagName in archetype.Tags) {
            tags.Add(new InspectorTag { TagName = tagName.tagName });
        }
        SetComponents(entity);
        SetScripts(entity);
    }
    
    private void SetComponents(Entity entity)
    {
        var components  = inspector.Components.Children;
        components.Clear();  // todo optimize
        var archetype   = entity.Archetype;
        
        foreach (var componentType in archetype.Structs)
        {
            if (!componentMap.TryGetValue(componentType, out var item)) {
                var component   = new InspectorComponent { ComponentTitle = componentType.type.Name };
                var panel       = new StackPanel();
                var fields      = AddComponentFields(componentType, panel);
                
                // <StackPanel IsVisible="{Binding #Comp1.Expanded}"
                var expanded = component.GetObservable(InspectorComponent.ExpandedProperty);
                // ^-- same as: AvaloniaObjectExtensions.GetObservable(component, InspectorComponent.ExpandedProperty);
                panel.Bind(Visual.IsVisibleProperty, expanded);
                
                item = new ComponentItem(component, panel, fields);
                componentMap.Add(componentType, item);
            }          
            var instance = entity.Archetype.GetEntityComponent(entity, componentType); // todo - instance is a struct -> avoid boxing
            ComponentField.SetComponentFields(item.fields, instance);
            
            components.Add(item.control);
            components.Add(item.panel);
        }
    }
    
    private void SetScripts(Entity entity)
    {
        var scripts = inspector.Scripts.Children;
        scripts.Clear();  // todo optimize
        
        foreach (var script in entity.Scripts)
        {
            var scriptType = script.GetType();
            if (!scriptMap.TryGetValue(scriptType, out var item)) {
                var component   = new InspectorComponent { ComponentTitle = script.GetType().Name };
                var panel       = new StackPanel();
                var fields      = AddScriptFields(script, panel);
                
                // <StackPanel IsVisible="{Binding #Comp1.Expanded}"
                var expanded = component.GetObservable(InspectorComponent.ExpandedProperty);
                panel.Bind(Visual.IsVisibleProperty, expanded);
                
                item = new ComponentItem(component, panel, fields);
                scriptMap.Add(scriptType, item);
            }
            ComponentField.SetScriptFields(item.fields, script);
            
            scripts.Add(item.control);
            scripts.Add(item.panel);
        }
    }
    
    /// <remarks><see cref="ComponentType.type"/> is a struct</remarks>
    private static ComponentField[] AddComponentFields(ComponentType componentType, Panel panel)
    {
        var fields = new List<ComponentField>();
        ComponentField.AddComponentFields(fields, componentType.type, null, null, default);
        AddFields(fields, panel);
        panel.Children.Add(new Separator());
        return fields.ToArray();
    }
    
    
    /// <remarks><paramref name="script"/> is a class</remarks>
    private static ComponentField[] AddScriptFields(Script script, Panel panel)
    {
        var scriptType  = script.GetType();
        var fields      = new List<ComponentField>();
        ComponentField.AddScriptFields(fields, scriptType);
        
        AddFields(fields, panel);
        panel.Children.Add(new Separator());
        return fields.ToArray();
    }
    
    private static void AddFields(List<ComponentField> fields, Panel panel)
    {
        foreach (var field in fields)
        {
            var dock    = new DockPanel();
            dock.Children.Add(new FieldName   { Text  = field.name } );
            dock.Children.Add(field.control);
            panel.Children.Add(dock);
        }
    }
}
