# RecursiveHelpOption Layout Comparison

This directory contains three alternative implementations of the RecursiveHelpOption feature, each with a different layout strategy for displaying command hierarchies.

## Layout Options

### 1. Compact Layout (`RecursiveHelpOption_Compact`)
**Best for**: Dense hierarchies where you want to see the structure at a glance

**Characteristics**:
- Uses indentation (2 spaces per level) to show hierarchy
- Shows only relative command names (not full paths) for subcommands
- Lists option/argument names on a single line without descriptions
- More condensed output

**Example**:
```
config
  Configure application settings
  get
    Get a configuration value
    Options: --key
  set
    Set a configuration value
    Options: --key, --value
```

### 2. Tree Layout (`RecursiveHelpOption_Tree`)
**Best for**: Visual clarity of hierarchy using tree characters

**Characteristics**:
- Uses tree characters (├──, └──, │) to show parent-child relationships
- Shows parameter counts instead of listing each parameter
- Very clear visual hierarchy
- Most visually appealing for complex hierarchies

**Example**:
```
config
  Configure application settings
├── get
│   Get a configuration value
│   Options: 1 option(s)
└── set
    Set a configuration value
    Options: 2 option(s)
```

### 3. Hierarchical Layout (`RecursiveHelpOption_Hierarchical`)
**Best for**: Copy-paste friendly full command paths

**Characteristics**:
- Shows full command paths for each command
- Uses indentation to show nesting
- Shows parameter signatures without descriptions
- Easy to copy full command names

**Example**:
```
config
  Configure application settings

  config get
    Get a configuration value
    Options: --key <key>
  config set
    Set a configuration value
    Options: --key <key> | --value <value>
```

## Testing the Layouts

Run the demo program with different layout options:

```bash
dotnet run --project RecursiveHelpLayoutDemo -- --layout=compact --help-all
dotnet run --project RecursiveHelpLayoutDemo -- --layout=tree --help-all
dotnet run --project RecursiveHelpLayoutDemo -- --layout=hierarchical --help-all
```

## Comparison Summary

| Feature | Compact | Tree | Hierarchical |
|---------|---------|------|--------------|
| Hierarchy visualization | Indentation | Tree characters | Indentation + full paths |
| Parameter details | Name list | Count only | Signature only |
| Vertical space | Minimal | Moderate | Moderate |
| Copy-paste friendly | Moderate | Low | High |
| Visual appeal | Low | High | Moderate |
| Best for | Dense hierarchies | Complex trees | Reference/documentation |

## Recommendation

Based on the goal of showing hierarchy while minimizing verbosity:
- **Tree layout** is recommended for its superior visual clarity
- **Compact layout** for maximum information density
- **Hierarchical layout** when users need to copy exact command names

The original implementation (flat with full paths and full descriptions) is kept as `RecursiveHelpOption.cs` for backward compatibility.
