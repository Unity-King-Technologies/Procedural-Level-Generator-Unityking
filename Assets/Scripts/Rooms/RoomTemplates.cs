using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomTemplates", menuName = "WFC/Room Templates")]
public class RoomTemplates : ScriptableObject
{
    [System.Serializable]
    public class RoomTemplate
    {
        public string templateName;
        public string description;
        public RoomTemplateType templateType;
        public List<RoomModule> requiredModules = new List<RoomModule>();
        public List<RoomModule> optionalModules = new List<RoomModule>();
        public Vector3Int dimensions = Vector3Int.one;
        public List<TemplateConstraint> constraints = new List<TemplateConstraint>();
        public List<TemplatePlacement> placements = new List<TemplatePlacement>();
        public float baseWeight = 1f;
        public List<string> tags = new List<string>();
    }

    [System.Serializable]
    public class TemplateConstraint
    {
        public Vector3Int relativePosition;
        public RoomModule requiredModule;
        public bool mustBeExactModule = false; // If false, allows compatible modules
        public float priority = 1f; // Higher priority constraints are enforced first
    }

    [System.Serializable]
    public class TemplatePlacement
    {
        public Vector3Int relativePosition;
        public RoomModule module;
        public bool isFixed = true; // If false, can be replaced during generation
        public float placementProbability = 1f;
    }

    public enum RoomTemplateType
    {
        StartArea,
        BossArena,
        TreasureRoom,
        PuzzleArea,
        Corridor,
        Junction,
        DeadEnd,
        Custom
    }

    public List<RoomTemplate> templates = new List<RoomTemplate>();

    // Runtime lookup
    private Dictionary<string, RoomTemplate> templateLookup;

    private void OnEnable()
    {
        BuildLookupTable();
    }

    private void BuildLookupTable()
    {
        templateLookup = new Dictionary<string, RoomTemplate>();
        foreach (var template in templates)
        {
            if (!templateLookup.ContainsKey(template.templateName))
            {
                templateLookup[template.templateName] = template;
            }
        }
    }

    /// <summary>
    /// Gets a template by name
    /// </summary>
    public RoomTemplate GetTemplate(string templateName)
    {
        if (templateLookup == null)
        {
            BuildLookupTable();
        }

        return templateLookup.ContainsKey(templateName) ? templateLookup[templateName] : null;
    }

    /// <summary>
    /// Gets all templates of a specific type
    /// </summary>
    public List<RoomTemplate> GetTemplatesByType(RoomTemplateType templateType)
    {
        return templates.FindAll(template => template.templateType == templateType);
    }

    /// <summary>
    /// Gets templates by tag
    /// </summary>
    public List<RoomTemplate> GetTemplatesByTag(string tag)
    {
        return templates.FindAll(template => template.tags.Contains(tag));
    }

    /// <summary>
    /// Selects a random template based on weights
    /// </summary>
    public RoomTemplate GetRandomTemplate(System.Random random = null)
    {
        if (templates.Count == 0)
            return null;

        if (random != null)
        {
            float totalWeight = templates.Sum(t => t.baseWeight);
            float randomValue = (float)random.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (var template in templates)
            {
                cumulative += template.baseWeight;
                if (randomValue <= cumulative)
                {
                    return template;
                }
            }
        }
        else
        {
            // Use Unity's random
            float totalWeight = templates.Sum(t => t.baseWeight);
            float randomValue = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var template in templates)
            {
                cumulative += template.baseWeight;
                if (randomValue <= cumulative)
                {
                    return template;
                }
            }
        }

        // Fallback
        return templates[0];
    }

    /// <summary>
    /// Applies a template to the grid at the specified position
    /// </summary>
    public bool ApplyTemplate(RoomTemplate template, Vector3Int position, Dictionary<Vector3Int, WFCCore.Cell> grid, RoomBank roomBank)
    {
        if (template == null || grid == null)
            return false;

        // Validate template can be applied
        if (!CanApplyTemplate(template, position, grid))
        {
            return false;
        }

        // Apply fixed placements first
        foreach (var placement in template.placements)
        {
            if (placement.isFixed && UnityEngine.Random.value <= placement.placementProbability)
            {
                Vector3Int placePos = position + placement.relativePosition;
                if (grid.ContainsKey(placePos))
                {
                    var cell = grid[placePos];
                    cell.possibleModules = new List<RoomModule> { placement.module };
                    cell.superpositionAmplitude = 0f; // Force this placement
                    cell.CalculateEntropy();
                }
            }
        }

        // Apply constraints
        var sortedConstraints = template.constraints.OrderByDescending(c => c.priority);
        foreach (var constraint in sortedConstraints)
        {
            Vector3Int constraintPos = position + constraint.relativePosition;
            if (grid.ContainsKey(constraintPos))
            {
                var cell = grid[constraintPos];

                if (constraint.mustBeExactModule)
                {
                    // Force exact module
                    cell.possibleModules = new List<RoomModule> { constraint.requiredModule };
                }
                else
                {
                    // Allow compatible modules
                    var compatibleModules = GetCompatibleModules(constraint.requiredModule, roomBank.GetAllModules());
                    cell.possibleModules = compatibleModules;
                }

                cell.superpositionAmplitude = 0f; // High priority constraint
                cell.CalculateEntropy();
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a template can be applied at the given position
    /// </summary>
    public bool CanApplyTemplate(RoomTemplate template, Vector3Int position, Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        if (template == null)
            return false;

        // Check if all required positions exist in grid
        foreach (var placement in template.placements)
        {
            Vector3Int placePos = position + placement.relativePosition;
            if (!grid.ContainsKey(placePos))
            {
                return false;
            }
        }

        foreach (var constraint in template.constraints)
        {
            Vector3Int constraintPos = position + constraint.relativePosition;
            if (!grid.ContainsKey(constraintPos))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets modules compatible with a given module
    /// </summary>
    private List<RoomModule> GetCompatibleModules(RoomModule referenceModule, List<RoomModule> allModules)
    {
        List<RoomModule> compatible = new List<RoomModule>();

        foreach (var module in allModules)
        {
            // Simple compatibility check - in a real implementation,
            // this would check socket compatibility
            if (module != null)
            {
                compatible.Add(module);
            }
        }

        return compatible;
    }

    /// <summary>
    /// Creates a template from an existing grid section
    /// </summary>
    public RoomTemplate CreateTemplateFromGrid(string templateName, Vector3Int startPosition, Vector3Int size,
        Dictionary<Vector3Int, WFCCore.Cell> grid, RoomTemplateType templateType = RoomTemplateType.Custom)
    {
        RoomTemplate template = new RoomTemplate
        {
            templateName = templateName,
            templateType = templateType,
            dimensions = size,
            placements = new List<TemplatePlacement>(),
            constraints = new List<TemplateConstraint>()
        };

        // Extract placements from grid
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    Vector3Int gridPos = startPosition + new Vector3Int(x, y, z);
                    Vector3Int relativePos = new Vector3Int(x, y, z);

                    if (grid.ContainsKey(gridPos))
                    {
                        var cell = grid[gridPos];
                        if (cell.isCollapsed && cell.collapsedModule != null)
                        {
                            template.placements.Add(new TemplatePlacement
                            {
                                relativePosition = relativePos,
                                module = cell.collapsedModule,
                                isFixed = true,
                                placementProbability = 1f
                            });
                        }
                    }
                }
            }
        }

        return template;
    }

    /// <summary>
    /// Validates template integrity
    /// </summary>
    public List<string> ValidateTemplate(RoomTemplate template)
    {
        List<string> errors = new List<string>();

        if (string.IsNullOrEmpty(template.templateName))
        {
            errors.Add("Template has no name");
        }

        if (template.dimensions.x <= 0 || template.dimensions.y <= 0 || template.dimensions.z <= 0)
        {
            errors.Add($"Template '{template.templateName}' has invalid dimensions");
        }

        // Check for overlapping placements and constraints
        HashSet<Vector3Int> positions = new HashSet<Vector3Int>();

        foreach (var placement in template.placements)
        {
            if (!positions.Add(placement.relativePosition))
            {
                errors.Add($"Template '{template.templateName}' has overlapping placements at {placement.relativePosition}");
            }
        }

        foreach (var constraint in template.constraints)
        {
            if (!positions.Add(constraint.relativePosition))
            {
                errors.Add($"Template '{template.templateName}' has overlapping constraints at {constraint.relativePosition}");
            }
        }

        return errors;
    }

    /// <summary>
    /// Adds a new template
    /// </summary>
    public void AddTemplate(RoomTemplate template)
    {
        if (!templates.Exists(t => t.templateName == template.templateName))
        {
            templates.Add(template);
            if (templateLookup != null)
            {
                templateLookup[template.templateName] = template;
            }
        }
    }

    /// <summary>
    /// Removes a template
    /// </summary>
    public bool RemoveTemplate(string templateName)
    {
        var templateToRemove = templates.Find(t => t.templateName == templateName);
        if (templateToRemove != null)
        {
            templates.Remove(templateToRemove);
            if (templateLookup != null)
            {
                templateLookup.Remove(templateName);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets template statistics
    /// </summary>
    public Dictionary<RoomTemplateType, int> GetTemplateStatistics()
    {
        Dictionary<RoomTemplateType, int> stats = new Dictionary<RoomTemplateType, int>();

        foreach (var template in templates)
        {
            if (!stats.ContainsKey(template.templateType))
            {
                stats[template.templateType] = 0;
            }
            stats[template.templateType]++;
        }

        return stats;
    }

#if UNITY_EDITOR
    [ContextMenu("Validate All Templates")]
    private void EditorValidateAllTemplates()
    {
        int totalErrors = 0;

        foreach (var template in templates)
        {
            var errors = ValidateTemplate(template);
            if (errors.Count > 0)
            {
                Debug.LogError($"Template '{template.templateName}' has {errors.Count} errors:");
                foreach (var error in errors)
                {
                    Debug.LogError("  - " + error);
                }
                totalErrors += errors.Count;
            }
        }

        if (totalErrors == 0)
        {
            Debug.Log("All templates validated successfully!");
        }
        else
        {
            Debug.LogError($"Found {totalErrors} errors across all templates");
        }
    }

    [ContextMenu("Rebuild Lookup Table")]
    private void EditorRebuildLookupTable()
    {
        BuildLookupTable();
        Debug.Log("Template lookup table rebuilt!");
    }

    [ContextMenu("Log Template Statistics")]
    private void EditorLogStatistics()
    {
        var stats = GetTemplateStatistics();
        Debug.Log("Template Statistics:");
        foreach (var kvp in stats)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} templates");
        }
    }
#endif
}
