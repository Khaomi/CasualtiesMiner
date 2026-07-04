using CasualtiesMiner.Uploader.Data.Mappers;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Bucket table definitions. Each entry is a page in the <c>Bucket:</c> namespace whose content is the
/// JSON schema described at https://meta.weirdgloop.org/w/Extension:Bucket.
/// </summary>
internal static class BucketSchemas
{
    /// <summary>
    /// The lightweight reference table that lists every item and its category.
    /// </summary>
    public const string IndexItemBucket = "Item";
    public const string LiquidContainerBucket = "Item_liquid";
    public const string BatteryBucket = "Item_battery";

    public const string LiquidBucket = "Liquid";

    public const string BlockBucket = "Block";

    public const string RecipeBucket = "Recipe";
    public const string RecipeIngridientBucket = "Recipe_ingridient";
    public const string RecipeResultBucket = "Recipe_result";

    public const string MoodleBucket = "Moodle";

    public const string GameFieldBucket = "Gamefield";
    public const string BodyFieldBucket = "Bodyfield";

    public const string BuildingBucket = "Building";

    private const string IndexSchema =
        """
        {
            "item_id":     { "type": "TEXT" },
            "category":    { "type": "TEXT" },
            "subtype":     { "type": "TEXT" },
            "weight":      { "type": "DOUBLE" },
            "value":       { "type": "INTEGER" },
            "tags":        { "type": "TEXT", "repeated": true },
            "usable":      { "type": "BOOLEAN" },
            "wearable":    { "type": "BOOLEAN" },
            "combineable": { "type": "BOOLEAN" },
            "obtainable":  { "type": "BOOLEAN" }
        }
        """;

    /// <summary>
    /// Full item per-category schema. The same definition is used for every category bucket.
    /// </summary>
    private const string CategorySchema =
        """
        {
            "item_id":                     { "type": "TEXT" },
            "weight":                      { "type": "DOUBLE" },
            "value":                       { "type": "INTEGER" },
            "tags":                        { "type": "TEXT", "repeated": true },
            "qualities":                   { "type": "TEXT", "repeated": true },
            "slot_rotation":               { "type": "DOUBLE", "index": false },
            "usable":                      { "type": "BOOLEAN" },
            "usable_on_limb":              { "type": "BOOLEAN" },
            "usable_with_lmb":             { "type": "BOOLEAN" },
            "auto_attack":                 { "type": "BOOLEAN" },
            "only_hold_in_hands":          { "type": "BOOLEAN" },
            "combineable":                 { "type": "BOOLEAN" },
            "destroy_at_zero_condition":   { "type": "BOOLEAN" },
            "scale_weight_with_condition": { "type": "BOOLEAN" },
            "ignore_depression":           { "type": "BOOLEAN" },
            "rot_speed":                   { "type": "DOUBLE" },
            "decay_minutes":               { "type": "DOUBLE" },
            "decay_info":                  { "type": "INTEGER" },
            "rec":                         { "type": "INTEGER" },
            "wearable":                    { "type": "BOOLEAN" },
            "wearable_can_be_held":        { "type": "BOOLEAN" },
            "wear_slot_id":                { "type": "TEXT" },
            "desired_wear_limb":           { "type": "TEXT" },
            "wearable_armor":              { "type": "DOUBLE" },
            "wearable_isolation":          { "type": "DOUBLE" },
            "wear_hit_dur_loss_mult":      { "type": "DOUBLE", "index": false },
            "jump_height_mult_change":     { "type": "DOUBLE", "index": false },
            "wearable_visual_offset":      { "type": "INTEGER", "index": false }
        }
        """;

    private const string LiquidItemSchema =
        """
        {
            "item_id":          { "type": "TEXT" },
            "capacity":         { "type": "DOUBLE" },
            "auto_fill":        { "type": "BOOLEAN" },
            "default_contents": { "type": "TEXT", "repeated": true }
        }
        """;

    private const string BatteryItemSchema =
        """
        {
            "item_id":    { "type": "TEXT" },
            "max_charge": { "type": "DOUBLE" }
        }
        """;

    /// <summary>
    /// Full liquid schema.
    /// </summary>
    private const string LiquidSchema =
        """
        {
            "liquid_id":          { "type": "TEXT" },
            "locale_name":        { "type": "TEXT" },
            "color":              { "type": "TEXT" },
            "value_per_liter":    { "type": "DOUBLE" },
            "health_usable":      { "type": "BOOLEAN" },
            "injectable":         { "type": "BOOLEAN" },
            "locale_from_item":   { "type": "BOOLEAN" },
            "injection_sickness": { "type": "DOUBLE" },
            "qualities":          { "type": "TEXT", "repeated": true }
        }
        """;

    /// <summary>
    /// Full block schema.
    /// </summary>
    private const string BlockSchema =
        """
        {
            "name":         { "type": "TEXT" },
            "health":       { "type": "DOUBLE" },
            "toxicity":     { "type": "DOUBLE" },
            "hitsound":     { "type": "TEXT" },
            "stepsound":    { "type": "TEXT" },
            "no_variation": { "type": "BOOLEAN" },
            "metallic":     { "type": "BOOLEAN" },
            "slippery":     { "type": "BOOLEAN" },
            "sleep":        { "type": "TEXT" }
        }
        """;

    /// <summary>
    /// Full recipe schema.
    /// </summary>
    private const string RecipeSchema =
        """
        {
            "recipe_id":         { "type": "TEXT" },
            "int":               { "type": "INTEGER" },
            "category":          { "type": "TEXT", "index": false },
            "is_repair":         { "type": "BOOLEAN" },
            "index":             { "type": "INTEGER" }
        }
        """;

    private const string RecipeIngridientSchema =
        """
        {
            "recipe_id":         { "type": "TEXT" },
            "specific":          { "type": "BOOLEAN" },
            "specific_id":       { "type": "TEXT", "index": false },
            "is_liquid":         { "type": "BOOLEAN" },
            "quality":           { "type": "TEXT", "repeated": true },
            "minimum_condition": { "type": "DOUBLE" },
            "destroy_item":      { "type": "BOOLEAN" },
            "ignored_id":        { "type": "TEXT", "index": false }
        }
        """;

    private const string RecipeResultSchema =
        """
        {
            "recipe_id":                { "type": "TEXT" },
            "amount":                   { "type": "INTEGER" }, 
            "dont_drain_result_liquid": { "type": "BOOLEAN" },
            "id":                       { "type": "TEXT", "index": false },
            "is_liquid":                { "type": "BOOLEAN" },
            "result_condition":         { "type": "DOUBLE" }
        }
        """;

    /// <summary>
    /// Full moodle schema.
    /// </summary>
    private const string MoodleSchema =
        """
        {
            "locale_id":               { "type": "TEXT" },
            "icon":                    { "type": "TEXT" },
            "icon_src_size":           { "type": "INTEGER", "index": false },
            "desc_locale_key":         { "type": "TEXT", "index": false },
            "precondition_for_moodle": { "type": "TEXT", "index": false },
            "precondition_display":    { "type": "TEXT", "index": false },
            "intensity":               { "type": "INTEGER", "index": false },
            "intensity_body_field_id": { "type": "TEXT", "index": false },
            "critical":                { "type": "BOOLEAN" },
            "critical_expr":           { "type": "TEXT", "index": false },
            "chipped_only":            { "type": "BOOLEAN" }
        }
        """;

    private const string GameFieldSchema =
        """
        {
            "game_field_id": { "type": "TEXT" },
            "value":         { "type": "DOUBLE" }
        }
        """;

    private const string BodyFieldSchema =
        """
        {
            "body_field_id":              { "type": "TEXT" },
            "label":                      { "type": "TEXT", "index": false },
            "kind":                       { "type": "TEXT" },
            "heal_speed_field_id":        { "type": "TEXT", "index": false },
            "max_timer_field_id":         { "type": "TEXT", "index": false },
            "intensity_scale_field_id":   { "type": "TEXT", "index": false },
            "splint_multiplier_field_id": { "type": "TEXT", "index": false }
        }
        """;

    /// <summary>
    /// Full building schema.
    /// </summary>
    private const string BuildingFieldSchema =
        """
        {
            "building_id":             { "type": "TEXT" },
            "items_drop_on_destroy":   { "type": "TEXT", "repeated": true },
            "health":                  { "type": "DOUBLE" },
            "require_ground":          { "type": "BOOLEAN" },
            "skip_description_set":    { "type": "BOOLEAN" },
            "drop_chance_multiplier":  { "type": "DOUBLE" },
            "guaranteed_drop_amount":  { "type": "DOUBLE" },
            "always_drop":             { "type": "TEXT", "repeated": true },
            "item_categories_to_add":  { "type": "TEXT", "repeated": true },
            "block_footstep_sound_id": { "type": "INTEGER" },
            "cant_hit":                { "type": "BOOLEAN" },
            "animal":                  { "type": "BOOLEAN" },
            "ignore_body_optimize":    { "type": "BOOLEAN" },
            "metallic":                { "type": "BOOLEAN" }
        }
        """;

    /// <summary>
    /// Bucket name for a given game category (e.g. <c>medical</c> -&gt; <c>Item_medical</c>).
    /// </summary>
    public static string CategoryBucket(string category)
    {
        return "Item_" + category;
    }

    /// <summary>
    /// All schema pages keyed by their bucket name (page title is <c>Bucket:{name}</c>).
    /// </summary>
    public static IReadOnlyList<(string Bucket, string Schema)> All()
    {
        var pages = new List<(string, string)>
        {
            (IndexItemBucket, IndexSchema),
            (LiquidContainerBucket, LiquidItemSchema),
            (BatteryBucket, BatteryItemSchema),
            (LiquidBucket, LiquidSchema),
            (BlockBucket, BlockSchema),
            (RecipeBucket, RecipeSchema),
            (RecipeIngridientBucket, RecipeIngridientSchema),
            (RecipeResultBucket, RecipeResultSchema),
            (MoodleBucket, MoodleSchema),
            (GameFieldBucket, GameFieldSchema),
            (BodyFieldBucket, BodyFieldSchema),
            (BuildingBucket, BuildingFieldSchema),
        };

        foreach (var category in ItemRowMapper.Categories)
        {
            pages.Add((CategoryBucket(category), CategorySchema));
        }

        return pages;
    }
}
