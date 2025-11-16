#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatsForge
{
    public enum TabType { Database, AttributeSets, Testing }

    public class AttributesEditorWindow : EditorWindow
    {
        private TabType currentTab = TabType.Database;
        private readonly Dictionary<TabType, ITabController> tabControllers = new();
        
        public readonly WindowState windowState = new();
        
        [MenuItem("Tools/StatsForge/Attributes Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<AttributesEditorWindow>("Attributes Editor");
            window.minSize = new Vector2(1000, 650);
            window.Show();
        }

        private void OnEnable()
        {
            windowState.Initialize();
            InitializeTabControllers();
        }

        private void InitializeTabControllers()
        {
            tabControllers[TabType.Database] = new DatabaseTabController(windowState);
            tabControllers[TabType.AttributeSets] = new AttributeSetsTabController(windowState);
            tabControllers[TabType.Testing] = new TestingTabController(windowState);
        }

        private void OnGUI()
        {
            if (!windowState.IsValid())
            {
                DatabaseSetupView.Draw(windowState);
                return;
            }

            using (new EditorGUILayout.VerticalScope())
            {
                TabNavigationView.Draw(ref currentTab, windowState);
                
                // Usar altura fixa para evitar barras de rolagem desnecessárias
                var contentHeight = position.height - 60; // Reserva espaço para toolbar e status bar
                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(contentHeight)))
                {
                    tabControllers[currentTab].DrawTab();
                }
                
                StatusBarView.Draw(windowState);
            }
        }

        private void Update()
        {
            if (windowState.AutoRefresh && windowState.ShouldRefresh())
            {
                windowState.RefreshData();
                Repaint();
            }
        }
    }

    public interface ITabController
    {
        void DrawTab();
    }

    public class WindowState
    {
        public AttributeDatabase Database { get; private set; }
        public List<AttributeSet> AttributeSets { get; private set; } = new();
        public AttributeType SelectedAttribute { get; set; }
        public AttributeSet SelectedAttributeSet { get; set; }
        public GameObject TestGameObject { get; set; }
        public bool AutoRefresh { get; set; } = true;
        
        private float lastRefreshTime;
        private const float REFRESH_INTERVAL = 1f;

        public void Initialize()
        {
            Database = AttributeDatabase.Instance;
            RefreshData();
        }

        public bool IsValid() => Database != null;
        
        public bool ShouldRefresh() => Time.realtimeSinceStartup - lastRefreshTime > REFRESH_INTERVAL;

        public void RefreshData()
        {
            if (Database == null) return;
            RefreshAttributeSets();
            lastRefreshTime = Time.realtimeSinceStartup;
        }

        private void RefreshAttributeSets()
        {
            var previousSelection = SelectedAttributeSet;
            AttributeSets.Clear();
            
            var guids = AssetDatabase.FindAssets("t:AttributeSet");
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var set = AssetDatabase.LoadAssetAtPath<AttributeSet>(path);
                if (set != null) 
                {
                    AttributeSets.Add(set);
                }
            }
            
            AttributeSets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            
            if (previousSelection != null && AttributeSets.Contains(previousSelection))
            {
                SelectedAttributeSet = previousSelection;
            }
            else if (SelectedAttributeSet != null && !AttributeSets.Contains(SelectedAttributeSet))
            {
                SelectedAttributeSet = null;
            }
        }
    }

    public static class TabNavigationView
    {
        private static readonly Color ACCENT_COLOR = new(0.4f, 0.6f, 0.8f);
        
        public static void Draw(ref TabType currentTab, WindowState state)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(20)))
            {
                var tabNames = new[] { "Database", "Attribute Sets", "Testing" };
                var tabValues = new[] { TabType.Database, TabType.AttributeSets, TabType.Testing };
                
                for (int i = 0; i < tabNames.Length; i++)
                {
                    var isSelected = currentTab == tabValues[i];
                    
                    if (isSelected) GUI.backgroundColor = ACCENT_COLOR;
                    
                    if (GUILayout.Button(tabNames[i], EditorStyles.toolbarButton))
                        currentTab = tabValues[i];
                    
                    GUI.backgroundColor = Color.white;
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Atualizar", EditorStyles.toolbarButton))
                {
                    state.RefreshData();
                    GUI.changed = true;
                }
            }
        }
    }

    public static class StatusBarView
    {
        public static void Draw(WindowState state)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(20)))
            {
                var selectedInfo = GetSelectedInfo(state);
                EditorGUILayout.LabelField(selectedInfo, EditorStyles.toolbarTextField);
                
                GUILayout.FlexibleSpace();
                
                if (state.TestGameObject != null)
                {
                    EditorGUILayout.LabelField($"Teste: {state.TestGameObject.name}", EditorStyles.toolbarTextField);
                }
            }
        }

        private static string GetSelectedInfo(WindowState state)
        {
            if (state.SelectedAttribute != null)
                return $"Selecionado: {state.SelectedAttribute.Name}";
            
            if (state.SelectedAttributeSet != null)
            {
                var displayName = UIHelper.GetDisplayName(state.SelectedAttributeSet.name);
                return $"Set: {displayName}";
            }
            
            return "Nenhum item selecionado";
        }
    }

    public static class DatabaseSetupView
    {
        public static void Draw(WindowState state)
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(400)))
                {
                    EditorGUILayout.LabelField("AttributeDatabase Não Encontrado", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Crie um AttributeDatabase para começar a usar o sistema.", MessageType.Error);
                    
                    EditorGUILayout.Space(10);
                    
                    if (GUILayout.Button("Criar AttributeDatabase", GUILayout.Height(30)))
                    {
                        DatabaseCreator.CreateDatabase();
                        state.Initialize();
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }
    }

    public class DatabaseTabController : ITabController
    {
        private readonly WindowState state;
        private readonly AttributeListView attributeListView;
        private readonly AttributeDetailsView attributeDetailsView;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        public DatabaseTabController(WindowState state)
        {
            this.state = state;
            this.attributeListView = new AttributeListView(state);
            this.attributeDetailsView = new AttributeDetailsView(state);
        }

        public void DrawTab()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(420)))
            {
                leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
                attributeListView.Draw();
                EditorGUILayout.EndScrollView();
            }
            
            DrawVerticalLine();
            
            using (new EditorGUILayout.VerticalScope())
            {
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
                attributeDetailsView.Draw();
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawVerticalLine()
        {
            var rect = GUILayoutUtility.GetRect(2, 0, GUILayout.ExpandHeight(true), GUILayout.Width(2));
            EditorGUI.DrawRect(rect, Color.gray);
        }
    }

    public class AttributeSetsTabController : ITabController
    {
        private readonly WindowState state;
        private readonly AttributeSetListView setListView;
        private readonly AttributeSetEditorView setEditorView;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        public AttributeSetsTabController(WindowState state)
        {
            this.state = state;
            this.setListView = new AttributeSetListView(state);
            this.setEditorView = new AttributeSetEditorView(state);
        }

        public void DrawTab()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(380)))
            {
                leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
                setListView.Draw();
                EditorGUILayout.EndScrollView();
            }
            
            DrawVerticalLine();
            
            using (new EditorGUILayout.VerticalScope())
            {
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
                setEditorView.Draw();
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawVerticalLine()
        {
            var rect = GUILayoutUtility.GetRect(2, 0, GUILayout.ExpandHeight(true), GUILayout.Width(2));
            EditorGUI.DrawRect(rect, Color.gray);
        }
    }

    public class TestingTabController : ITabController
    {
        private readonly WindowState state;
        private readonly TestingControlsView controlsView;
        private readonly TestingVisualizationView visualizationView;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        public TestingTabController(WindowState state)
        {
            this.state = state;
            this.controlsView = new TestingControlsView(state);
            this.visualizationView = new TestingVisualizationView(state);
        }

        public void DrawTab()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(380)))
            {
                leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
                controlsView.Draw();
                EditorGUILayout.EndScrollView();
            }
            
            DrawVerticalLine();
            
            using (new EditorGUILayout.VerticalScope())
            {
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
                visualizationView.Draw();
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawVerticalLine()
        {
            var rect = GUILayoutUtility.GetRect(2, 0, GUILayout.ExpandHeight(true), GUILayout.Width(2));
            EditorGUI.DrawRect(rect, Color.gray);
        }
    }

    // Continuação das outras classes com o mesmo padrão...
    public class AttributeListView
    {
        private readonly WindowState state;
        private readonly AttributeCreationView creationView;
        private readonly AttributeFilterView filterView;

        public AttributeListView(WindowState state)
        {
            this.state = state;
            this.creationView = new AttributeCreationView(state);
            this.filterView = new AttributeFilterView();
        }

        public void Draw()
        {
            creationView.Draw();
            filterView.Draw(state.Database);
            DrawAttributesList();
        }

        private void DrawAttributesList()
        {
            if (state.Database?.AllAttributes == null) return;
            
            var filteredAttributes = FilterAttributes();
            
            foreach (var categoryGroup in filteredAttributes)
            {
                UIHelper.DrawCard($"{categoryGroup.Key} ({categoryGroup.Count()})", () =>
                {
                    foreach (var attr in categoryGroup.OrderBy(a => a.Name))
                    {
                        DrawAttributeItem(attr);
                    }
                }, true);
            }
        }

        private IEnumerable<IGrouping<string, AttributeType>> FilterAttributes()
        {
            return state.Database.AllAttributes
                .Where(a => a != null)
                .Where(a => string.IsNullOrEmpty(filterView.SearchFilter) || 
                           a.Name.Contains(filterView.SearchFilter, StringComparison.OrdinalIgnoreCase))
                .Where(a => string.IsNullOrEmpty(filterView.SelectedCategory) || a.Category == filterView.SelectedCategory)
                .GroupBy(a => a.Category)
                .OrderBy(g => g.Key);
        }

        private void DrawAttributeItem(AttributeType attr)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (state.SelectedAttribute == attr)
                {
                    var rect = GUILayoutUtility.GetRect(4, EditorGUIUtility.singleLineHeight);
                    EditorGUI.DrawRect(rect, new Color(0.4f, 0.6f, 0.8f, 0.3f));
                }
                
                if (GUILayout.Button(attr.Name, EditorStyles.label))
                    state.SelectedAttribute = attr;
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Deletar", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    AttributeOperations.DeleteAttribute(state, attr);
                }
            }
        }
    }

    public class AttributeDetailsView
    {
        private readonly WindowState state;
        private string editedName = "";
        private string editedCategory = "";
        private bool isEditingName;
        private bool isEditingCategory;

        public AttributeDetailsView(WindowState state)
        {
            this.state = state;
        }

        public void Draw()
        {
            if (state.SelectedAttribute == null)
            {
                UIHelper.DrawEmptySelection("Selecione um atributo para ver detalhes");
                return;
            }

            UIHelper.DrawCard($"Editando: {state.SelectedAttribute.Name}", () =>
            {
                EditorGUILayout.LabelField("Informações Básicas", EditorStyles.boldLabel);
                
                DrawNameEditor();
                DrawCategoryEditor();
                
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("Alterar Categoria (Menu)"))
                {
                    CategoryEditor.ShowCategoryMenu(state.SelectedAttribute, state.Database);
                }
                
                if (GUILayout.Button("Localizar Asset"))
                {
                    EditorGUIUtility.PingObject(state.SelectedAttribute);
                }
            });
        }

        private void DrawNameEditor()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Nome:", GUILayout.Width(80));
                
                if (!isEditingName)
                {
                    EditorGUILayout.LabelField(state.SelectedAttribute.Name);
                    if (GUILayout.Button("Editar", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        isEditingName = true;
                        editedName = state.SelectedAttribute.Name;
                    }
                }
                else
                {
                    editedName = EditorGUILayout.TextField(editedName);
                    
                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(editedName) || editedName == state.SelectedAttribute.Name);
                    if (GUILayout.Button("Salvar", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        state.SelectedAttribute.SetName(editedName.Trim());
                        EditorUtility.SetDirty(state.SelectedAttribute);
                        EditorUtility.SetDirty(state.Database);
                        AssetDatabase.SaveAssets();
                        isEditingName = false;
                    }
                    EditorGUI.EndDisabledGroup();
                    
                    if (GUILayout.Button("Cancelar", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        isEditingName = false;
                    }
                }
            }
        }

        private void DrawCategoryEditor()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Categoria:", GUILayout.Width(80));
                
                if (!isEditingCategory)
                {
                    EditorGUILayout.LabelField(state.SelectedAttribute.Category);
                    if (GUILayout.Button("Editar", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        isEditingCategory = true;
                        editedCategory = state.SelectedAttribute.Category;
                    }
                }
                else
                {
                    editedCategory = EditorGUILayout.TextField(editedCategory);
                    
                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(editedCategory) || editedCategory == state.SelectedAttribute.Category);
                    if (GUILayout.Button("Salvar", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        state.SelectedAttribute.SetCategory(editedCategory.Trim());
                        EditorUtility.SetDirty(state.SelectedAttribute);
                        EditorUtility.SetDirty(state.Database);
                        AssetDatabase.SaveAssets();
                        isEditingCategory = false;
                    }
                    EditorGUI.EndDisabledGroup();
                    
                    if (GUILayout.Button("Cancelar", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        isEditingCategory = false;
                    }
                }
            }
        }
    }

    public class AttributeCreationView
    {
        private readonly WindowState state;
        private string newAttributeName = "";
        private string newAttributeCategory = "Core";

        public AttributeCreationView(WindowState state)
        {
            this.state = state;
        }

        public void Draw()
        {
            UIHelper.DrawCard("Criar Novo Atributo", () =>
            {
                newAttributeName = EditorGUILayout.TextField("Nome:", newAttributeName);
                newAttributeCategory = EditorGUILayout.TextField("Categoria:", newAttributeCategory);
                
                EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(newAttributeName));
                
                if (GUILayout.Button("Criar Atributo", GUILayout.Height(25)))
                {
                    CreateAttribute();
                }
                
                EditorGUI.EndDisabledGroup();
            });
        }

        private void CreateAttribute()
        {
            var newAttr = state.Database.CreateAttribute(newAttributeName.Trim(), newAttributeCategory.Trim());
            if (newAttr != null)
            {
                state.SelectedAttribute = newAttr;
                newAttributeName = "";
                EditorUtility.SetDirty(state.Database);
                AssetDatabase.SaveAssets();
            }
            else
            {
                EditorUtility.DisplayDialog("Erro", "Não foi possível criar o atributo. Verifique se o nome já existe.", "OK");
            }
        }
    }

    public class AttributeFilterView
    {
        private string searchFilter = "";
        private string selectedCategory = "";
        
        public string SearchFilter => searchFilter;
        public string SelectedCategory => selectedCategory;

        public void Draw(AttributeDatabase database)
        {
            if (database == null) return;
            
            UIHelper.DrawCard("Filtros", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    searchFilter = EditorGUILayout.TextField("Buscar:", searchFilter);
                    if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        searchFilter = "";
                        GUI.FocusControl(null);
                    }
                }
                
                var categories = new[] { "Todas" }.Concat(database.Categories).ToArray();
                var selectedIndex = Array.IndexOf(categories, string.IsNullOrEmpty(selectedCategory) ? "Todas" : selectedCategory);
                selectedIndex = EditorGUILayout.Popup("Categoria:", selectedIndex == -1 ? 0 : selectedIndex, categories);
                selectedCategory = selectedIndex == 0 ? "" : categories[selectedIndex];
            });
        }
    }

    public class AttributeSetListView
    {
        private readonly WindowState state;
        private string searchFilter = "";
        private AttributeSet renamingSet;
        private string newSetName = "";

        public AttributeSetListView(WindowState state)
        {
            this.state = state;
        }

        public void Draw()
        {
            DrawControls();
            DrawSetsList();
        }

        private void DrawControls()
        {
            UIHelper.DrawCard("Controles", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Sets: {state.AttributeSets.Count}");
                    
                    if (GUILayout.Button("Criar Novo", GUILayout.Width(80)))
                    {
                        AttributeSetOperations.CreateNew(state);
                    }
                    
                    if (GUILayout.Button("Atualizar", GUILayout.Width(70)))
                    {
                        state.RefreshData();
                    }
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    var newSearchFilter = EditorGUILayout.TextField("Filtrar:", searchFilter);
                    if (newSearchFilter != searchFilter)
                    {
                        searchFilter = newSearchFilter;
                    }
                    
                    if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        searchFilter = "";
                        GUI.FocusControl(null);
                    }
                }
            });
        }

        private void DrawSetsList()
        {
            // Create a copy of the collection to avoid modification during enumeration
            var filteredSets = state.AttributeSets
                .Where(s => s != null && (string.IsNullOrEmpty(searchFilter) || 
                           UIHelper.GetDisplayName(s.name).Contains(searchFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList(); // Convert to list to avoid enumeration issues

            if (!filteredSets.Any())
            {
                EditorGUILayout.HelpBox("Nenhum AttributeSet encontrado.", MessageType.Info);
                return;
            }

            foreach (var set in filteredSets)
            {
                DrawSetItem(set);
            }
        }

        private void DrawSetItem(AttributeSet set)
        {
            if (set == null) return;
            
            var isSelected = state.SelectedAttributeSet == set;
            
            if (renamingSet == set)
            {
                DrawRenameMode(set);
            }
            else
            {
                DrawNormalMode(set, isSelected);
            }
        }

        private void DrawNormalMode(AttributeSet set, bool isSelected)
        {
            using (new EditorGUILayout.VerticalScope(isSelected ? GUI.skin.box : GUIStyle.none))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Área clicável para seleção - mais ampla
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        var displayName = UIHelper.GetDisplayName(set.name);
                        
                        if (GUILayout.Button(displayName, EditorStyles.label, GUILayout.ExpandWidth(true)))
                            state.SelectedAttributeSet = set;
                        
                        EditorGUILayout.LabelField($"{set.Attributes.Count} atributos", EditorStyles.miniLabel);
                    }
                    
                    // Botões de ação - largura fixa
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(150)))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Renomear", EditorStyles.miniButton, GUILayout.Width(70)))
                            {
                                renamingSet = set;
                                // Remover o prefixo "so_" para edição
                                newSetName = UIHelper.GetDisplayName(set.name);
                            }
                            
                            if (GUILayout.Button("Deletar", EditorStyles.miniButton, GUILayout.Width(65)))
                            {
                                AttributeSetOperations.Delete(state, set);
                            }
                        }
                        
                        if (GUILayout.Button("Localizar", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
                        {
                            Selection.activeObject = set;
                            EditorGUIUtility.PingObject(set);
                        }
                    }
                }
            }
            EditorGUILayout.Space(2);
        }

        private void DrawRenameMode(AttributeSet set)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Renomeando Set:", EditorStyles.miniLabel);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("so_", GUILayout.Width(25));
                    newSetName = EditorGUILayout.TextField(newSetName);
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(newSetName) || 
                                                UIHelper.GetDisplayName(set.name) == newSetName);
                    if (GUILayout.Button("Salvar", EditorStyles.miniButton))
                    {
                        if (AttributeSetOperations.Rename(state, set, newSetName.Trim()))
                        {
                            renamingSet = null;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    
                    if (GUILayout.Button("Cancelar", EditorStyles.miniButton))
                    {
                        renamingSet = null;
                    }
                }
            }
        }
    }

    public class AttributeSetEditorView
    {
        private readonly WindowState state;

        public AttributeSetEditorView(WindowState state)
        {
            this.state = state;
        }

        public void Draw()
        {
            if (state.SelectedAttributeSet == null)
            {
                UIHelper.DrawEmptySelection("Selecione um AttributeSet para editar");
                return;
            }

            DrawSetInfo();
            DrawAttributesInSet();
        }

        private void DrawSetInfo()
        {
            var displayName = UIHelper.GetDisplayName(state.SelectedAttributeSet.name);
                
            UIHelper.DrawCard($"Editando: {displayName}", () =>
            {
                EditorGUILayout.LabelField($"Atributos: {state.SelectedAttributeSet.Attributes.Count}");
                EditorGUILayout.LabelField($"Arquivo: {System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(state.SelectedAttributeSet))}");
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Adicionar Atributo", GUILayout.ExpandWidth(true)))
                    {
                        AttributeSetOperations.ShowAddAttributeMenu(state);
                    }
                    
                    if (state.SelectedAttributeSet.Attributes.Count > 0 && 
                        GUILayout.Button("Limpar Tudo", GUILayout.Width(100)))
                    {
                        if (EditorUtility.DisplayDialog("Limpar Todos Atributos",
                            "Remover todos os atributos deste set?", "Limpar", "Cancelar"))
                        {
                            AttributeSetOperations.ClearAll(state.SelectedAttributeSet);
                        }
                    }
                }
                
                if (GUILayout.Button("Localizar Asset"))
                {
                    Selection.activeObject = state.SelectedAttributeSet;
                    EditorGUIUtility.PingObject(state.SelectedAttributeSet);
                }
            });
        }

        private void DrawAttributesInSet()
        {
            if (state.SelectedAttributeSet.Attributes.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhum atributo neste set.", MessageType.Info);
                return;
            }

            var groupedAttrs = state.SelectedAttributeSet.Attributes
                .Where(entry => entry.type != null)
                .GroupBy(entry => entry.type.Category)
                .OrderBy(g => g.Key);

            foreach (var group in groupedAttrs)
            {
                var foldoutKey = $"set_{state.SelectedAttributeSet.GetInstanceID()}_{group.Key}";
                var isExpanded = EditorPrefs.GetBool(foldoutKey, true);
                var newExpanded = EditorGUILayout.Foldout(isExpanded, $"{group.Key} ({group.Count()})", true);
                EditorPrefs.SetBool(foldoutKey, newExpanded);

                if (newExpanded)
                {
                    EditorGUI.indentLevel++;
                    foreach (var entry in group.OrderBy(e => e.type.Name))
                    {
                        DrawAttributeEntry(entry);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawAttributeEntry(AttributeSet.AttributeEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField(entry.type.Name, GUILayout.Width(120));
                
                EditorGUILayout.LabelField("Valor:", GUILayout.Width(50));
                var newValue = EditorGUILayout.FloatField(entry.baseValue, GUILayout.Width(60));
                if (Math.Abs(newValue - entry.baseValue) > 0.001f)
                {
                    state.SelectedAttributeSet.AddAttribute(entry.type, newValue);
                    EditorUtility.SetDirty(state.SelectedAttributeSet);
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Remover", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    state.SelectedAttributeSet.RemoveAttribute(entry.type);
                    EditorUtility.SetDirty(state.SelectedAttributeSet);
                }
            }
        }
    }

    public class TestingControlsView
    {
        private readonly WindowState state;
        private float customModifierValue = 10f;
        private float customDuration = 5f;
        private ModifierType customModifierType = ModifierType.Flat;
        private string customSource = "Test";

        public TestingControlsView(WindowState state)
        {
            this.state = state;
        }

        public void Draw()
        {
            DrawGameObjectSelection();
            
            if (IsValidTestObject())
            {
                DrawCustomModifiers();
                DrawQuickControls();
            }
        }

        private void DrawGameObjectSelection()
        {
            UIHelper.DrawCard("Configuração de Teste", () =>
            {
                state.TestGameObject = EditorGUILayout.ObjectField("GameObject:", 
                    state.TestGameObject, typeof(GameObject), true) as GameObject;
                
                if (state.TestGameObject == null)
                {
                    EditorGUILayout.HelpBox("Selecione um GameObject para testar.", MessageType.Info);
                    return;
                }

                var entityAttributes = state.TestGameObject.GetComponent<EntityAttributes>();
                
                if (entityAttributes == null)
                {
                    EditorGUILayout.HelpBox("GameObject precisa do componente EntityAttributes.", MessageType.Warning);
                    if (GUILayout.Button("Adicionar EntityAttributes"))
                    {
                        state.TestGameObject.AddComponent<EntityAttributes>();
                    }
                }
                else
                {
                    var setName = entityAttributes.CurrentAttributeSet ? 
                        UIHelper.GetDisplayName(entityAttributes.CurrentAttributeSet.name) : "Nenhum";
                    EditorGUILayout.LabelField($"Set Atual: {setName}");
                    
                    if (GUILayout.Button("Trocar Set"))
                    {
                        TestingOperations.ShowChangeSetMenu(state);
                    }
                }
            });
        }

        private void DrawCustomModifiers()
        {
            UIHelper.DrawCard("Modificadores Customizados", () =>
            {
                customModifierType = (ModifierType)EditorGUILayout.EnumPopup("Tipo:", customModifierType);
                customModifierValue = EditorGUILayout.FloatField("Valor:", customModifierValue);
                customDuration = EditorGUILayout.FloatField("Duração:", customDuration);
                customSource = EditorGUILayout.TextField("Origem:", customSource);
                
                if (GUILayout.Button("Aplicar Modificador"))
                {
                    TestingOperations.ApplyCustomModifier(state, customModifierType, 
                        customModifierValue, customDuration, customSource);
                }
            });
        }

        private void DrawQuickControls()
        {
            UIHelper.DrawCard("Controles Rápidos", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Resetar Tudo"))
                    {
                        TestingOperations.ResetAllAttributes(state);
                    }
                    
                    if (GUILayout.Button("Limpar Modificadores"))
                    {
                        TestingOperations.ClearAllModifiers(state);
                    }
                }
            });
        }

        private bool IsValidTestObject()
        {
            return state.TestGameObject != null && 
                   state.TestGameObject.GetComponent<EntityAttributes>() != null;
        }
    }

    public class TestingVisualizationView
    {
        private readonly WindowState state;

        public TestingVisualizationView(WindowState state)
        {
            this.state = state;
        }

        public void Draw()
        {
            if (!IsValidTestObject())
            {
                UIHelper.DrawEmptySelection("Configure um GameObject para testar");
                return;
            }

            var entityAttrs = state.TestGameObject.GetComponent<EntityAttributes>();
            DrawAttributesOverview(entityAttrs);
            DrawAttributesDetails(entityAttrs);
        }

        private void DrawAttributesOverview(EntityAttributes entityAttrs)
        {
            UIHelper.DrawCard("Visão Geral dos Atributos", () =>
            {
                EditorGUILayout.LabelField($"Atributos: {entityAttrs.AttributeCount}");
                var setName = entityAttrs.CurrentAttributeSet ? 
                    UIHelper.GetDisplayName(entityAttrs.CurrentAttributeSet.name) : "Nenhum";
                EditorGUILayout.LabelField($"Set: {setName}");
            });
        }

        private void DrawAttributesDetails(EntityAttributes entityAttrs)
        {
            if (entityAttrs.CurrentAttributeSet == null)
            {
                EditorGUILayout.HelpBox("Nenhum AttributeSet configurado.", MessageType.Warning);
                return;
            }

            var attributeGroups = GetAttributeGroups(entityAttrs);

            foreach (var group in attributeGroups)
            {
                var foldoutKey = $"test_{group.Key}";
                var isExpanded = EditorPrefs.GetBool(foldoutKey, true);
                var newExpanded = EditorGUILayout.Foldout(isExpanded, $"{group.Key} ({group.Count()})", true);
                EditorPrefs.SetBool(foldoutKey, newExpanded);

                if (newExpanded)
                {
                    EditorGUI.indentLevel++;
                    foreach (var attr in group.OrderBy(a => a.Name))
                    {
                        DrawAttributeTestControls(entityAttrs, attr.Name);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private IEnumerable<IGrouping<string, AttributeInfo>> GetAttributeGroups(EntityAttributes entityAttrs)
        {
            return entityAttrs.GetAttributeNames()
                .Select(name => new AttributeInfo { Name = name, Type = state.Database.GetAttribute(name) })
                .Where(x => x.Type != null)
                .GroupBy(x => x.Type.Category)
                .OrderBy(g => g.Key);
        }

        private void DrawAttributeTestControls(EntityAttributes entityAttrs, string attributeName)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                DrawAttributeHeader(entityAttrs, attributeName);
                DrawAttributeControls(entityAttrs, attributeName);
            }
        }

        private void DrawAttributeHeader(EntityAttributes entityAttrs, string attributeName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(attributeName, EditorStyles.boldLabel, GUILayout.Width(120));
                
                var baseValue = entityAttrs.GetBaseValue(attributeName);
                var currentValue = entityAttrs.GetValue(attributeName);
                var modifierCount = entityAttrs.GetModifierCount(attributeName);
                
                EditorGUILayout.LabelField($"Base: {baseValue:F1}", GUILayout.Width(60));
                EditorGUILayout.LabelField($"Atual: {currentValue:F1}", GUILayout.Width(70));
                EditorGUILayout.LabelField($"Mods: {modifierCount}", GUILayout.Width(50));
                
                if (GUILayout.Button("Limpar", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    entityAttrs.ClearAllModifiers(attributeName);
                }
            }
        }

        private void DrawAttributeControls(EntityAttributes entityAttrs, string attributeName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+5", EditorStyles.miniButton, GUILayout.Width(30)))
                    entityAttrs.AddFlat(attributeName, 5f, 3f, "Test");
                
                if (GUILayout.Button("+10", EditorStyles.miniButton, GUILayout.Width(30)))
                    entityAttrs.AddFlat(attributeName, 10f, 5f, "Test");
                
                if (GUILayout.Button("-5", EditorStyles.miniButton, GUILayout.Width(30)))
                    entityAttrs.AddFlat(attributeName, -5f, 3f, "Test");
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("+25%", EditorStyles.miniButton, GUILayout.Width(40)))
                    entityAttrs.AddPercentage(attributeName, 25f, 5f, "Test");
                
                if (GUILayout.Button("x2", EditorStyles.miniButton, GUILayout.Width(30)))
                    entityAttrs.AddMultiplier(attributeName, 100f, 5f, "Test");
            }
        }

        private bool IsValidTestObject()
        {
            return state.TestGameObject != null && 
                   state.TestGameObject.GetComponent<EntityAttributes>() != null;
        }

        private class AttributeInfo
        {
            public string Name { get; set; }
            public AttributeType Type { get; set; }
        }
    }

    public static class UIHelper
    {
        public static void DrawCard(string title, Action content, bool foldout = false)
        {
            EditorGUILayout.Space(3);
            
            if (foldout)
            {
                var key = $"foldout_{title}";
                var isExpanded = EditorPrefs.GetBool(key, true);
                var newExpanded = EditorGUILayout.Foldout(isExpanded, title, true);
                EditorPrefs.SetBool(key, newExpanded);
                
                if (!newExpanded) return;
            }
            else
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            }
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                content?.Invoke();
            }
        }

        public static void DrawEmptySelection(string message)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(message, EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
        }
        
        public static string GetDisplayName(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return "Unnamed";
            
            // Remove o prefixo "so_" para exibição
            if (assetName.StartsWith("so_"))
                return assetName.Substring(3);
            
            return assetName;
        }
    }

    public static class WindowStateRefresher
    {
        public static void RefreshAll()
        {
            var windows = Resources.FindObjectsOfTypeAll<AttributesEditorWindow>();
            foreach (var window in windows)
            {
                window.windowState.RefreshData();
                window.Repaint();
            }
        }
    }

    public static class DatabaseCreator
    {
        public static void CreateDatabase()
        {
            var resourcesPath = "Assets/Resources";
            var attributesPath = "Assets/Resources/Attributes";
            
            if (!AssetDatabase.IsValidFolder(resourcesPath))
                AssetDatabase.CreateFolder("Assets", "Resources");
                
            if (!AssetDatabase.IsValidFolder(attributesPath))
                AssetDatabase.CreateFolder(resourcesPath, "Attributes");
            
            var database = ScriptableObject.CreateInstance<AttributeDatabase>();
            AssetDatabase.CreateAsset(database, attributesPath + "/AttributeDatabase.asset");
            
            database.CreateAttribute("Health", "Core");
            database.CreateAttribute("Mana", "Core");
            database.CreateAttribute("Strength", "Combat");
            database.CreateAttribute("Dexterity", "Combat");
            database.CreateAttribute("Intelligence", "Magic");
            
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    public static class AttributeOperations
    {
        public static void DeleteAttribute(WindowState state, AttributeType attr)
        {
            if (EditorUtility.DisplayDialog("Deletar Atributo", 
                $"Deletar '{attr.Name}'?\n\nEsta ação não pode ser desfeita.", "Deletar", "Cancelar"))
            {
                if (state.SelectedAttribute == attr) 
                    state.SelectedAttribute = null;
                
                state.Database.RemoveAttribute(attr.Name);
                EditorUtility.SetDirty(state.Database);
                AssetDatabase.SaveAssets();
                state.RefreshData();
            }
        }
    }

    public static class AttributeSetOperations
    {
        public static void CreateNew(WindowState state)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Criar Attribute Set", "so_NewAttributeSet", "asset", "Salvar novo Attribute Set");
            
            if (!string.IsNullOrEmpty(path))
            {
                var newSet = ScriptableObject.CreateInstance<AttributeSet>();
                AssetDatabase.CreateAsset(newSet, path);
                AssetDatabase.SaveAssets();
                state.RefreshData();
                state.SelectedAttributeSet = newSet;
                Selection.activeObject = newSet;
            }
        }

        public static bool Rename(WindowState state, AttributeSet set, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return false;
            
            var oldPath = AssetDatabase.GetAssetPath(set);
            
            // Garantir que o nome tenha o prefixo "so_"
            var fullName = newName.StartsWith("so_") ? newName : $"so_{newName}";
            var fileName = fullName + ".asset";
            
            var result = AssetDatabase.RenameAsset(oldPath, fileName);
            if (!string.IsNullOrEmpty(result))
            {
                EditorUtility.DisplayDialog("Erro de Renomeação", result, "OK");
                return false;
            }
            else
            {
                AssetDatabase.SaveAssets();
                state.RefreshData();
                return true;
            }
        }

        public static void Delete(WindowState state, AttributeSet set)
        {
            var displayName = GetDisplayName(set.name);
            
            if (EditorUtility.DisplayDialog("Deletar Set", 
                $"Deletar '{displayName}'?\n\nEsta ação não pode ser desfeita.", "Deletar", "Cancelar"))
            {
                // Defer the deletion to avoid collection modification during GUI rendering
                EditorApplication.delayCall += () =>
                {
                    if (state.SelectedAttributeSet == set) 
                        state.SelectedAttributeSet = null;
                    
                    var assetPath = AssetDatabase.GetAssetPath(set);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                        state.RefreshData();
                    }
                };
            }
        }

        public static void ShowAddAttributeMenu(WindowState state)
        {
            var menu = new GenericMenu();
            var availableAttrs = state.Database.AllAttributes
                .Where(a => a != null && !state.SelectedAttributeSet.HasAttribute(a))
                .GroupBy(a => a.Category)
                .OrderBy(g => g.Key);

            foreach (var group in availableAttrs)
            {
                foreach (var attr in group.OrderBy(a => a.Name))
                {
                    menu.AddItem(new GUIContent($"{group.Key}/{attr.Name}"), false, () =>
                    {
                        state.SelectedAttributeSet.AddAttribute(attr, 0f);
                        EditorUtility.SetDirty(state.SelectedAttributeSet);
                    });
                }
            }
            
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("Nenhum atributo disponível"));
            }
            
            menu.ShowAsContext();
        }

        public static void ClearAll(AttributeSet set)
        {
            var attributesToRemove = set.Attributes.Select(e => e.type).Where(t => t != null).ToList();
            
            foreach (var attr in attributesToRemove)
            {
                set.RemoveAttribute(attr);
            }
            
            EditorUtility.SetDirty(set);
        }
        
        private static string GetDisplayName(string assetName)
        {
            return UIHelper.GetDisplayName(assetName);
        }
    }

    public static class CategoryEditor
    {
        public static void ShowCategoryMenu(AttributeType attr, AttributeDatabase database)
        {
            var menu = new GenericMenu();
            var categories = database.Categories.Concat(new[] { "Core", "Combat", "Social", "Crafting", "Magic" }).Distinct();
            
            foreach (var category in categories)
            {
                var isSelected = attr.Category == category;
                menu.AddItem(new GUIContent(category), isSelected, () =>
                {
                    if (!isSelected)
                    {
                        attr.SetCategory(category);
                        EditorUtility.SetDirty(attr);
                        EditorUtility.SetDirty(database);
                        AssetDatabase.SaveAssets();
                    }
                });
            }
            
            menu.ShowAsContext();
        }
    }

    public static class TestingOperations
    {
        public static void ShowChangeSetMenu(WindowState state)
        {
            var entityAttrs = state.TestGameObject.GetComponent<EntityAttributes>();
            var menu = new GenericMenu();
            
            foreach (var set in state.AttributeSets)
            {
                var displayName = UIHelper.GetDisplayName(set.name);
                var isCurrentSet = entityAttrs.CurrentAttributeSet == set;
                
                menu.AddItem(new GUIContent(displayName), isCurrentSet, () =>
                {
                    if (!isCurrentSet)
                    {
                        entityAttrs.SetAttributeSet(set);
                    }
                });
            }
            
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("Nenhum AttributeSet disponível"));
            }
            
            menu.ShowAsContext();
        }

        public static void ApplyCustomModifier(WindowState state, ModifierType type, 
            float value, float duration, string source)
        {
            var entityAttrs = state.TestGameObject.GetComponent<EntityAttributes>();
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            
            foreach (var attrName in entityAttrs.GetAttributeNames())
            {
                entityAttrs.ApplyModifier(attrName, type, value, application, duration, source);
            }
        }

        public static void ResetAllAttributes(WindowState state)
        {
            var entityAttrs = state.TestGameObject.GetComponent<EntityAttributes>();
            foreach (var attrName in entityAttrs.GetAttributeNames())
            {
                entityAttrs.ClearAllModifiers(attrName);
            }
        }

        public static void ClearAllModifiers(WindowState state)
        {
            var entityAttrs = state.TestGameObject.GetComponent<EntityAttributes>();
            entityAttrs.ClearAllModifiers();
        }
    }
}
#endif