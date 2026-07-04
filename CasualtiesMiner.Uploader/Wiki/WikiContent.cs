namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Hand-authored wiki pages (Lua router, locale resolver, trigger page).
/// Generated pages: <c>Module:Item/data</c>, <c>Module:Locale/&lt;lang&gt;/*</c>.
/// </summary>
internal static class WikiContent
{
    public const string LocaleModuleTitle = "Module:Locale";
    public const string WikiUiModuleTitle = "Module:Locale/WikiUi";

    public const string RouterItemModuleTitle = "Module:ItemData";
    public const string RouterLiquidModuleTitle = "Module:LiquidData";
    public const string RouterBlockModuleTitle = "Module:BlockData";
    public const string RouterRecipeModuleTitle = "Module:RecipeData";
    public const string RouterRecipeItemModuleTitle = "Module:RecipeItemData";
    public const string RouterRecipeResultModuleTitle = "Module:RecipeResultData";
    public const string RouterMoodleModuleTitle = "Module:MoodleData";
    public const string RouterBuildingModuleTitle = "Module:BuildingData";
    public const string RouterGameFieldModuleTitle = "Module:GameFieldData";
    public const string RouterBodyFieldModuleTitle = "Module:BodyFieldData";

    public const string ItemBucketModuleTitle = "Module:ItemBucket";
    public const string BlockBucketModuleTitle = "Module:BlockBucket";
    public const string LiquidBucketModuleTitle = "Module:LiquidBucket";
    public const string RecipeBucketModuleTitle = "Module:RecipeBucket";
    public const string MoodleBucketModuleTitle = "Module:MoodleBucket";
    public const string BuildingBucketModuleTitle = "Module:BuildingBucket";

    public const string ItemDataModuleTitle = "Module:Item/data";
    public const string LiquidDataModuleTitle = "Module:Liquid/data";
    public const string BlockDataModuleTitle = "Module:Block/data";
    public const string RecipeDataModuleTitle = "Module:Recipe/data";
    public const string RecipeItemDataModuleTitle = "Module:RecipeItem/data";
    public const string RecipeResultDataModuleTitle = "Module:RecipeResult/data";
    public const string MoodleDataModuleTitle = "Module:Moodle/data";
    public const string GameFieldDataModuleTitle = "Module:GameField/data";
    public const string BodyFieldDataModuleTitle = "Module:BodyField/data";
    public const string BuildingDataModuleTitle = "Module:Building/data";

    public const string TriggerItemPageTitle = "Project:Items data";
    public const string TriggerLiquidPageTitle = "Project:Liquid data";
    public const string TriggerBlockPageTitle = "Project:Block data";
    public const string TriggerRecipePageTitle = "Project:Recipe data";
    public const string TriggerRecipeItemPageTitle = "Project:RecipeItem data";
    public const string TriggerRecipeResultPageTitle = "Project:RecipeResult data";
    public const string TriggerMoodlePageTitle = "Project:Moodle data";
    public const string TriggerGameFieldPageTitle = "Project:GameField data";
    public const string TriggerBodyFieldPageTitle = "Project:BodyField data";
    public const string TriggerBuildingPageTitle = "Project:Building data";

    #region Locale

    /// <summary>
    /// Resolves localized item names, descriptions and UI labels at render time.
    /// </summary>
    public const string LocaleModule =
        """
        -- Module:Locale
        -- Runtime i18n: item strings live in Module:Locale/<LANG>/items, UI labels in .../ui,
        -- wiki-only expression labels in Module:Locale/WikiUi.
        -- Language is taken from the optional |lang= parameter, otherwise the wiki content language.

        local p = {}
        local DEFAULT = "EN"

        local wikiUiShared

        local function normalizeLang(code)
            if not code or code == "" then return DEFAULT end
            return mw.language.new(code):getCode():upper()
        end

        local function tryLoadData(moduleTitle)
            local ok, data = pcall(mw.loadData, moduleTitle)
            if ok and type(data) == "table" then return data end
            return nil
        end

        local function loadTable(lang, suffix)
            lang = normalizeLang(lang)
            return tryLoadData("Module:Locale/" .. lang .. "/" .. suffix)
                or tryLoadData("Module:Locale/" .. DEFAULT .. "/" .. suffix)
                or {}
        end

        function p.resolveLang(frame)
            local args = frame:getParent().args
            if args.lang and tostring(args.lang) ~= "" then
                return normalizeLang(args.lang)
            end
            return normalizeLang(mw.language.getContentLanguage():getCode())
        end

        ---Query item locale information in specified language.
        ---@param itemId string Item ID or display name. Case insensitive.
        ---@param lang any Target language. For example: EN
        ---@return table Item Item language table entry.
        function p.getItem(itemId, lang)
            if not itemId or itemId == "" then return nil end
            local tbl = loadTable(lang, "items")
            local itemIdLc = string.lower(itemId)
            local res = tbl[itemId] or tbl[itemIdLc]
            if res ~= nil then return res end

            for id, item in pairs(tbl) do
                if item.name == itemId or string.lower(item.name) == itemIdLc then return item end
            end
        end

        ---Query liquid locale information in specified language.
        ---@param liquidId string Liquid ID or display name. Case insensitive.
        ---@param lang any Target language. For example: EN
        ---@return table Liquid language table entry.
        function p.getLiquid(liquidId, lang)
            if not liquidId or liquidId == "" then return nil end
            local tbl = loadTable(lang, "liquids")
            local liquidIdLc = string.lower(liquidId)
            local res = tbl[liquidId] or tbl[liquidIdLc]
            if res ~= nil then return res end
        
            for id, liquid in pairs(tbl) do
                if liquid.name == liquidId or string.lower(liquid.name) == liquidIdLc then return liquid end
            end
        end
        
        ---Query building locale information in specified language.
        ---@param buildingId string Building ID or display name. Case insensitive.
        ---@param lang any Target language. For example: EN
        ---@return table Building language table entry.
        function p.getBuilding(buildingId, lang)
            if not buildingId or buildingId == "" then return nil end
            local tbl = loadTable(lang, "buildings")
            local buildingIdLc = string.lower(buildingId)
            local res = tbl[buildingId] or tbl[buildingIdLc]
            if res ~= nil then return res end
        
            for id, building in pairs(tbl) do
                if building.name == buildingId or string.lower(building.name) == buildingIdLc then return building end
            end
        end

        ---Query moodle locale information in specified language.
        ---@param moodleId string Moodle ID. Case insensitive.
        ---@param lang any Target language. For example: EN
        ---@return table Moodle language table entry.
        function p.getMoodle(moodleId, lang)
            if not moodleId or moodleId == "" then return nil end
            local tbl = loadTable(lang, "moodles")
            local moodleIdLc = string.lower(moodleId)
            local res = tbl[moodleId] or tbl[moodleIdLc]
            if res ~= nil then return res end
        
            for id, moodle in pairs(tbl) do
                if moodle.locale_id == moodleId or string.lower(moodle.locale_id) == moodleIdLc then return moodle end
            end
        end

        function p.ui(key, lang)
            local ui = loadTable(lang, "ui")
            return ui[key] or key
        end

        function p.wikiUi(lang)
            local perLang = loadTable(lang, "WikiUi")
            if perLang and next(perLang) ~= nil then
                return perLang
            end
            if wikiUiShared == nil then
                wikiUiShared = tryLoadData("Module:Locale/WikiUi") or {}
            end
            return wikiUiShared
        end

        function p.bodyField(path, lang)
            local tbl = p.wikiUi(lang)
            return tbl[path] or path
        end

        function p.localizeExpr(expr, lang)
            if not expr or expr == "" then return expr end
            local tbl = p.wikiUi(lang)
            local paths = {}
            for path in pairs(tbl) do
                if type(path) == "string" and path:find("^body%.") then
                    paths[#paths + 1] = path
                end
            end
            table.sort(paths, function(a, b) return #a > #b end)
            for _, path in ipairs(paths) do
                local label = tbl[path]
                if label and label ~= "" then
                    local escaped = path:gsub("([%.%(%)%[%]%-%+])", "%%%1")
                    expr = expr:gsub(escaped, label)
                end
            end
            if tbl.critical then
                expr = expr:gsub("%f[%a]critical%f[%A]", tbl.critical)
            end
            return mw.text.trim(expr)
        end

        function p.categoryName(category, lang)
            return p.ui("cat_" .. (category or "custom"), lang)
        end

        function p.itemName(frame)
            local itemId = frame.args[1] or frame.args.item_id
            local lang = p.resolveLang(frame)
            local item = p.getItem(itemId, lang)
            return item and item.name or itemId
        end

        function p.itemDescription(frame)
            local itemId = frame.args[1] or frame.args.item_id
            local lang = p.resolveLang(frame)
            local item = p.getItem(itemId, lang)
            return item and item.description or ""
        end

        function p.uiLabel(frame)
            local key = frame.args[1] or frame.args.key
            return p.ui(key, p.resolveLang(frame))
        end

        return p
        """;

    #endregion Locale

    #region Items

    /// <summary>
    /// Reads a row from Bucket + locale strings, formats fields (weight, decay, rec, wear slots),
    /// and expands <c>Template:Item Infobox</c>. Keep in sync with the live wiki module.
    /// </summary>
    public const string ItemBucketModule =
        """
        -- Module:ItemBucket
        -- Usage on a page: {{#invoke:ItemBucket|infobox|shotgun}}

        local Locale = require("Module:Locale")
        local getArgs = require("Module:Arguments").getArgs
        local liquidBucket = require("Module:LiquidBucket")
        local bit32 = require( 'bit32' )
        local yesNo = require("Module:Yesno")
        local tmpRenderer = require("Module:TMPRender")

        -- if true, enables debug printing. to be used when editing this module.
        local DEBUG = false

        local p = {}
        local lang = mw.getContentLanguage()

        local DETAIL_COLUMNS = {
            "item_id", "weight", "value", "tags", "qualities", "slot_rotation",
            "usable", "usable_on_limb", "usable_with_lmb", "auto_attack", "only_hold_in_hands",
            "combineable", "destroy_at_zero_condition", "scale_weight_with_condition",
            "ignore_depression", "decay_minutes", "rot_speed", "decay_info", "rec",
            "wearable", "wearable_can_be_held", "wear_slot_id", "desired_wear_limb",
            "wearable_armor", "wearable_isolation", "wear_hit_dur_loss_mult",
            "jump_height_mult_change", "wearable_visual_offset",
        }

        local function merge(into, from)
            if not from then return end
            for k, v in pairs(from) do into[k] = v end
        end

        local function firstRow(result)
            return result and result[1] or nil
        end

        function p.fetch(itemId)
            local index = firstRow(bucket("item")
                .select("item_id", "category", "subtype", "weight", "value", "tags", "usable", "wearable", "combineable", "obtainable")
                .where("item_id", itemId)
                .run())

            if not index then return nil end

            local row = {}
            merge(row, index)

            local category = row.category or "custom"
            local detail = firstRow(bucket("item_" .. category)
                .select(unpack(DETAIL_COLUMNS))
                .where("item_id", itemId)
                .run())
            merge(row, detail)

            if row.subtype == "liquid" then
                merge(row, firstRow(bucket("item_liquid")
                    .select("capacity", "auto_fill", "default_contents")
                    .where("item_id", itemId)
                    .run()))
            elseif row.subtype == "battery" then
                merge(row, firstRow(bucket("item_battery")
                    .select("max_charge")
                    .where("item_id", itemId)
                    .run()))
            end

            return row
        end

        function capitalizeFirst(str)
            return (str:gsub("^%l", string.upper))
        end

        local function paramValue(v)
            if v == nil then return "" end
            if type(v) == "boolean" then
                return yesNo(v) and "true" or "false"
            end
            if type(v) == "table" then
                return table.concat(v, ", ")
            end
            return tostring(v)
        end

        local function escapeTemplate(v)
            return paramValue(v):gsub("|", "{{!}}")
        end

        local function assert_not_nil(value, error_message)
            if value == nil then
                if error_message == nil then
                    error("value is nil")
                else
                    error(error_message)
                end
            end

            return value
        end

        local function reduce_num_table(tbl, reduceFn, initial_value)
            local out = initial_value

            for i, v in ipairs(tbl) do
                out = reduceFn(out, v, i, tbl)
            end

            return out
        end

        local function map_numeric_table(tbl, mapFn)
            local res = {}
            for _, v in ipairs(tbl) do
                table.insert(res, mapFn(v, i, tbl))
            end
            return res
        end

        local function round(num)
            return math.floor(num + 0.5)
        end

        -- Source - https://stackoverflow.com/a/67917761
        -- Posted by George Williams, modified by community. See post 'Timeline' for change history
        -- Retrieved 2026-05-25, License - CC BY-SA 4.0
        local function round_to_digit(num, dp)
            --[[
            round a number to so-many decimal of places, which can be negative,
            e.g. -1 places rounds to 10's,

            examples
                173.2562 rounded to 0 dps is 173.0
                173.2562 rounded to 2 dps is 173.26
                173.2562 rounded to -1 dps is 170.0
            ]]--
            local mult = 10^(dp or 0)

            return math.floor(num * mult + 0.5)/mult
        end

        local function format_decay(minutes)
            local durationStr = lang:formatDuration(minutes * 60)
            return tostring(mw.html.create("span")
                :wikitext("<b>[[File:Icon_decay.png|16px|class=pixelated]]&nbsp;" .. durationStr .. "</b>"))
        end

        --- Converts list array props `{ "prop1:prop2:propN", "prop1:prop2:propN"  }` to table html elements.
        --- @param args table Args.
        --- @param args.caption string Optional table caption. Example: `"Contents"`
        --- @param args.headers table Optional table headers. Example: `{ "Substance", "Amount (mL)" }`
        --- @param args.rows table Required table rows. Each row is expected to be separated with a delimiter. Example: `{ "epinephrine:15", "oxyline:25" }`
        --- @param args.delimiter string Optional delimiter. `":"` by default. Example: `":"`
        --- @param args.process function Optional processing function. Called on each cell.
        --- @param args.process.args table `process` function args.
        --- @param args.process.args.column number Column. Begins with 1.
        --- @param args.process.args.value string Cell value.
        --- @param args.process.args.rowsCount number Amount of rows the table has in total.
        --- @param args.postprocess function Optional post-processing function. Called once after the table is processed.
        --- @param args.postprocess.args table `postprocess` function args.
        --- @param args.postprocess.args.rows table Processed rows. Each row is a table with values of either string or any other type resulting from the processing step.
        --- @return unknown element HTML element.
        local function listToTableEl(args)
            assert_not_nil(args.rows, "'rows' was not provided")
            args.delimiter = args.delimiter or ":"

            local containerEl = mw.html.create("table")

            if args.caption then containerEl:tag("caption"):wikitext(args.caption) end

            if args.headers then
                local headRow = containerEl:tag("tr")
                for _, item in ipairs(args.headers) do
                    headRow:tag("th"):wikitext(item)
                end
            end

            local rows = {}

            -- process rows
            for _, item in ipairs(args.rows) do
                local row = {}
                for column, value in ipairs(mw.text.split(item, args.delimiter, true)) do
                    if args.process then
                        value = args.process{
                            column = column,
                            value = value,
                            rowsCount = #args.rows
                        }
                    end

                    table.insert(row, value)
                end
                table.insert(rows, row)
            end

            -- postprocess rows
            if args.postprocess then
                args.postprocess{
                    rows = rows
                }
            end

            -- generate row elements
            for _, row in ipairs(rows) do
                local rowEl = containerEl:tag("tr")
                for _, value in ipairs(row) do
                    rowEl:tag("td"):wikitext(value)
                end
            end

            return containerEl
        end

        -- =================================================
        function p.infobox(frame)
            local args = getArgs(frame)

            local addExtraMargin = yesNo(args.extra_margin or false)

            local itemId = args.item_id or args[1] or args["1"]
            itemId = itemId and mw.text.trim(tostring(itemId)) or ""
            if itemId == "" then
                return "[[Category:Errors]]<strong>ItemBucket:</strong> missing item id."
            end

            local row = p.fetch(itemId)
            if not row then
                return "[[Category:Errors]]<strong>ItemBucket:</strong> no Bucket row for '" .. itemId .. "'."
            end

            local lang = Locale.resolveLang(frame)
            local localeItem = Locale.getItem(itemId, lang)

            if DEBUG then 
                mw.log("> lang")
                mw.logObject(lang)
                mw.log("> localeItem")
                mw.logObject(localeItem)
                mw.log("> args")
                mw.logObject(args)
                mw.log("> row")
                mw.logObject(row)
            end

            local resArgs = {
                item_id = itemId,
                display_name = localeItem and localeItem.name or itemId,
                description = tmpRenderer.render_tmp_text(localeItem and localeItem.description or ""),
                extra_margin = addExtraMargin and "yes" or "no"
            }
            local yesTemplate = tostring(frame:expandTemplate{ title = "yes" })

            for key, value in pairs(row) do
                if key == "decay_minutes" or key == "rot_speed" then
                    -- - decayMinutes = minutes it takes for the item to decay. can sometimes be 0 and rotSpeed be non zero - see rotSpeed.
                    -- - rotSpeed = 1.666f / decayMinutes (ie you could say it duplicates decayMinutes - in most cases).
                    -- decayMinutes can be calculated back via "(100 / rotSpeed) / 60" (+round to get rid of float err).
                    -- correlates with decayMinutes UNLESS decayMinutes is 0, then this value is used. if both are 0, then they are 0
                    -- rotSpeed can also be NEGATIVE, which means that the item doesn't decay - instead it regenerates.

                    local decayMinutes = row.decay_minutes
                    local rotSpeed = row.rot_speed

                    if decayMinutes and decayMinutes ~= 0 then
                        resArgs.decay_duration = format_decay(decayMinutes)
                    elseif rotSpeed and rotSpeed ~= 0 then
                        decayMinutes = round_to_digit((100 / rotSpeed) / 60, 1)

                        if decayMinutes >= 0 then
                            resArgs.decay_duration = format_decay(decayMinutes)
                        else
                            resArgs.regenerate_duration = format_decay(-decayMinutes)
                        end
                    end
                elseif key == "decay_info" then
                    -- decayInfo = flag
                    -- public enum DecayType : byte
                    -- {
                    --     NoDecayWithoutContainerItem = 1, - doesn't decay when doesn't have items inside
                    --     NoDecayWhenNotWorn = 2, doesn't decay when not worn
                    --     NoDecayWhenStill = 4, doesn't decay when standing still
                    --     BatteryDecay = 0x10 = uses charge instead of using hp for decay (stuff like flashlight, gravbag)
                    -- }

                    local decayInfo = value

                    if bit32.btest(decayInfo, 1) then resArgs.no_decay_when_empty_as_container = yesTemplate end
                    if bit32.btest(decayInfo, 2) then resArgs.no_decay_when_not_worn = yesTemplate end
                    if bit32.btest(decayInfo, 4) then resArgs.no_decay_when_standing_still = yesTemplate end
                    if bit32.btest(decayInfo, 8) then resArgs.battery_charge_as_decay = yesTemplate end
                elseif key == "weight" then
                    local weight = row.weight
                    local scaleWeightWithCondition = yesNo(row.scale_weight_with_condition)

                    local res = ""
                    if weight then
                        res = "{{ui icon|weight|" .. weight .. "}}"

                        if scaleWeightWithCondition then
                            res = res .. " " .. '<br><span style="color: gray; filter: contrast(25%);">{{ui icon|weight|0.1}} <sub style="vertical-align: middle;">(minimal)</sub></span>'
                        end
                    end
                    resArgs.weight = frame:preprocess(res)
                elseif key == "rec" then
                    local rec = value

                    if rec == 0 then
                        resArgs.rec_min = "<span style='color: #fc007c;'>None required</span>"
                    else
                        local container = mw.html.create("div")
                            :addClass("rec-grid")

                        for i = 1, 20 do
                            local cell = container:tag("div")

                            if i <= rec then
                                cell:addClass("f")
                            end
                        end

                        local wrapper = mw.html.create("div")
                            :wikitext("<span style='color: #fc007c;'>'''INT "..rec.." required'''</span>\n")
                            :node(container)

                        resArgs.rec_min = tostring(wrapper)
                    end
                elseif key == "default_contents" then
                    function process (args)
                        -- column: Substance 
                        if args.column == 1 then
                            local liquidId = args.value
                            local liquidLoc = Locale.getLiquid(liquidId, lang)
                            local liquidRow = liquidBucket.fetch(liquidId)
                            local name = liquidLoc and liquidLoc.name or nil

                            local res
                            if name then
                                local page = name.." (liquid)"
                                res = "[[" .. page .. "|" .. name .. "]]"
                            else
                                res = name
                            end

                            local color = liquidRow.color or "transparent"
                            return '<span style="--liquid-col: '..color..';">'..res..'</span>'
                        -- column: Amount 
                        elseif args.column == 2 then
                            local valueNum = tonumber(args.value)
                            if valueNum == nil then error("failed to parse amount in 'default_contents' to number; value: " .. args.value) end
                            args.value = valueNum
                        end

                        return args.value
                    end

                    function postprocess(args)
                        if #args.rows < 2 then return end

                        local amounts = map_numeric_table(args.rows, function (row)
                            -- column: Amount 
                            return row[2]
                        end)

                        local totalAmount = reduce_num_table(amounts, function (acc, value)
                            -- can add bcs converted to number in process step
                            return acc + value
                        end, 0)

                        for _, row in ipairs(args.rows) do
                            for column, value in ipairs(row) do
                                -- column: Amount 
                                if column == 2 then
                                    local percentage = round_to_digit(value / totalAmount * 100, 1)
                                    -- add how much of the resulting sludge the substance composes
                                    row[column] = value .. " <sup style='color: var(--text-subtle); font-size: .7em;'>("..percentage.."%)</sup>"
                                end
                            end
                        end
                    end

                    resArgs[key] = tostring(listToTableEl{
                        caption = frame:preprocess("{{ui tooltip|Contents|Default contents of this container.}}"),
                        headers = { "Substance", "Amount (mL)" }, 
                        rows = value, 
                        process = process,
                        postprocess = postprocess
                    })
                elseif key == "qualities" then
                    local qualityLcToCategoryMap = {
                        cutting = "[[Category:Tools]]",
                        hammering = "[[Category:Tools]]",
                    }

                    function process (args)
                        -- column: Quality 
                        if args.column == 1 then
                            local label = capitalizeFirst(args.value)
                            local page = "Category:Quality: "..label
                            local res = "[[:"..page.."|"..label.."]]"
                            local qualityCategory = "[["..page.."]]"
                            local qualityCategory2 = qualityLcToCategoryMap[string.lower(args.value)] or ""
                            return res .. qualityCategory2 .. qualityCategory
                        -- column: Count 
                        elseif args.column == 2 then
                            return args.value or "1"
                        end

                        return args.value
                    end

                function postprocess(args)
                        for _, row in ipairs(args.rows) do
                            if #row < 2 then
                                -- set 1 amount to columns where amount is not set. 1 is the default.
                                row[2] = 1
                            end
                        end
                    end

                    resArgs[key] = tostring(listToTableEl{
                        caption = frame:preprocess("{{ui icon|quality|{{ui tooltip|Qualities|Specific characteristics of this item.}}}}"),
                        headers = { "Quality", "Count" },
                        rows = value,
                        process = process,
                        postprocess = postprocess
                    })
                elseif key == "wearable_armor" then
                    if value ~= 0 then
                        resArgs.wearable_armor = value

                        local damageReduction = 1 - 1 / (1 + value)
                        damageReductionFmted = frame:expandTemplate{ title = "ui icon", args = { "armor", round_to_digit(damageReduction, 1) * 100 } }
                        resArgs.damage_reduction = damageReductionFmted
                    end
                else
                    resArgs[key] = paramValue(value)
                end
            end

            if DEBUG then 
                mw.log("> resArgs")
                mw.logObject(resArgs)
            end

            return frame:expandTemplate{ title = "Item Infobox", args = resArgs }
        end

        -- Renders N infoboxes. For debugging purposes.
        function p.n_infoboxes(frame)
            local n = tonumber(frame.args.n) or 1
            local from = tonumber(frame.args.from) or 1

            local parent = mw.html.create("div")
                :css("display", "flex")
                :css("flex-direction", "row")
                :css("flex-wrap", "wrap")

            local queryRes = bucket("item")
                .select("item_id")
                .run()

            local total = #queryRes

            local count = 0
            for _, obj in ipairs(queryRes) do
                count = count + 1
                if count >= from then
                    local itemId = obj.item_id
                    frame.args[1] = itemId
                    parent:node(p.infobox(frame))
                end

                if count == (from + n) then break end
            end

            return "Displaying " .. from .. " to " .. count .. " of " .. total, parent
        end

        return p
        """;

    public const string RouterItemModule =
        """
        -- Module:ItemData
        -- Routes language-neutral item rows into Bucket tables.
        -- Localized names/descriptions are resolved at render time via Module:Locale.

        local Locale = require("Module:Locale")

        local p = {}

        local function putRow(r)
            bucket("item").put(r)
            bucket("item_" .. (r.category or "custom")).put(r)
            if r.subtype == "liquid" then bucket("item_liquid").put(r) end
            if r.subtype == "battery" then bucket("item_battery").put(r) end
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:Item/data")
            for _, row in ipairs(data) do
                putRow(row)
            end
            return string.format("Stored %d items into Bucket.", #data)
        end

        return p
        """;

    #endregion Items

    #region Liquids

    /// <summary>
    /// Reads a row from Bucket + locale strings, formats fields (weight, decay, rec, wear slots),
    /// and expands <c>Template:Item Infobox</c>. Keep in sync with the live wiki module.
    /// </summary>
    public const string LiquidBucketModule =
        """
        local Locale = require("Module:Locale")
        local getArgs = require("Module:Arguments").getArgs
        local yesNo = require("Module:Yesno")

        local p = {}

        local function firstRow(result)
            return result and result[1] or nil
        end

        local function paramValue(v)
            if v == nil then return "" end
            if type(v) == "boolean" then
                return yesNo(v) and "true" or "false"
            end
            if type(v) == "table" then
                return table.concat(v, ", ")
            end
            return tostring(v)
        end

        function p.fetch(liquidId)
            return firstRow(bucket("liquid")
                .select("liquid_id", "color", "value_per_liter", "health_usable",
                        "injectable", "locale_from_item", "injection_sickness", "qualities")
                .where("liquid_id", liquidId)
                .run())
        end

        -- =================================================
        function p.infobox(frame)
            local args = getArgs(frame)

            local liquidId = args.liquid_id or args[1] or args["1"]
            liquidId = liquidId and mw.text.trim(tostring(liquidId)) or ""
            if liquidId == "" then
                return "[[Category:Errors]]<strong>LiquidBucket:</strong> missing liquid id."
            end
        
            local row = p.fetch(liquidId)
            if not row then
                return "[[Category:Errors]]<strong>LiquidBucket:</strong> no Bucket row for '" .. liquidId .. "'."
            end

            local lang = Locale.resolveLang(frame)
            local localeLiquid = Locale.getLiquid(liquidId, lang)
        
            if DEBUG then 
                mw.log("> lang")
                mw.logObject(lang)
                mw.log("> localeLiquid")
                mw.logObject(localeLiquid)
                mw.log("> args")
                mw.logObject(args)
                mw.log("> row")
                mw.logObject(row)
            end

            local resArgs = {
                liquid_id = liquidId,
                display_name = localeLiquid and localeLiquid.name or liquidId,
                description = localeLiquid and localeLiquid.description or "",
            }

            for key, value in pairs(row) do
                resArgs[key] = paramValue(value)
            end

            local hex = row.color and mw.text.trim(tostring(row.color)) or ""
            hex = hex:gsub("^#", "")
            if hex ~= "" then
                resArgs.color_css = hex
            end

            return frame:expandTemplate{ title = "Liquid Infobox", args = resArgs }
        end

        -- Renders N infoboxes. For debugging purposes.
        function p.n_infoboxes(frame)
            local n = tonumber(frame.args.n) or 1
            local from = tonumber(frame.args.from) or 1
        
            local parent = mw.html.create("div")
                :css("display", "flex")
                :css("flex-direction", "row")
                :css("flex-wrap", "wrap")
        
            local queryRes = bucket("liquid")
                .select("liquid_id")
                .run()
        
            local total = #queryRes
        
            local count = 0
            for _, obj in ipairs(queryRes) do
                count = count + 1
                if count >= from then
                    local liquidId = obj.liquid_id
                    frame.args[1] = liquidId
                    parent:node(p.infobox(frame))
                end
        
                if count == (from + n) then break end
            end
        
            return "Displaying " .. from .. " to " .. count .. " of " .. total, parent
        end

        return p
        """;

    public const string RouterLiquidModule =
    """
        -- Module:LiquidData
        -- Routes language-neutral liquid rows into Bucket tables.
        -- Localized names/descriptions are resolved at render time via Module:Locale.

        local Locale = require("Module:Locale")

        local p = {}

        local function putRow(r)
            bucket("liquid").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:Liquid/data")
            for _, row in ipairs(data) do
                putRow(row)
            end

            return string.format("Stored %d liquids into Bucket.", #data)
        end

        return p
        """;

    #endregion Items

    #region Blocks

    public const string RouterBlockModule =
    """
        -- Module:BlockData
        -- Routes language-neutral recipe rows into Bucket tables.
        
        local p = {}

        local function putRow(r)
            bucket("block").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:Block/data")
            local count = 0
            for _, row in ipairs(data) do
                putRow(row)
                count = count + 1
            end
            return string.format("Stored %d blocks into Bucket.", count)
        end

        return p
        """;

    #endregion Blocks

    #region Recipes

    public const string RecipeBucketModule =
        """
        local Locale = require("Module:Locale")
        local getArgs = require("Module:Arguments").getArgs

        local p = {}

        local function firstRow(result)
            return result and result[1] or nil
        end

        function capitalizeFirst(str)
            return (str:gsub("^%l", string.upper))
        end

        function p.fetchResultItem(recipeId)
            return firstRow(bucket("recipe")
                .join("recipe_result", "recipe_result.recipe_id", "recipe_id")
                .select("recipe_id", 
                        "int", 
                        "category", 
                        "is_repair", 
                        "index",
                        "recipe_result.amount",
                        "recipe_result.id",
                        "recipe_result.is_liquid",
                        "recipe_result.result_condition"
                        )
                .where("recipe_id", recipeId)
                .run())
        end

        function p.fetchIngridients(recipeId)
            return bucket("recipe")
                .join("recipe_ingridient", "recipe_ingridient.recipe_id", "recipe_id")
                .select(
                        "recipe_ingridient.specific",
                        "recipe_ingridient.specific_id",
                        "recipe_ingridient.is_liquid",
                        "recipe_ingridient.quality",
                        "recipe_ingridient.minimum_condition",
                        "recipe_ingridient.destroy_item",
                        "recipe_ingridient.ignored_id"
                        )
                .where("recipe_id", recipeId)
                .run()
        end

        local function qualityLabel(entry)
            if type(entry) ~= "string" or entry == "" then
                return nil
            end
            local id, amount = entry:match("^([^:]+):?(.*)$")
            id = id or entry
            local n = tonumber(amount) or 1
            return n, id
        end

        local function formatNumber(value)
            local n = tonumber(value)
            if not n then
                return tostring(value)
            end
            if n == math.floor(n) then
                return string.format("%d", n)
            end
            local s = string.format("%.4f", n):gsub("0+$", ""):gsub("%.$", "")
            return s
        end

        local function qualityDisplayName(qualityId, lang)
            local key = "cq" .. qualityId

            local label = Locale.ui(key, lang)
            if label ~= key then
                return label
            end

            return capitalizeFirst(qualityId)
        end

        local function formatItemIconLink(itemId, linkTitle)
            linkTitle = linkTitle or itemId
            return "[[File:" .. itemId .. ".png|16x16px|class=pixelated]]"
        end

        local function fetchLiquidColor(liquidId)
            if not liquidId or liquidId == "" then
                return nil
            end

            local res = bucket("liquid")
                .select("liquid_id", "color")
                .where("liquid_id", liquidId)
                .run()

            local row = res and res[1]
            if not row or not row.color then
                return nil
            end

            local hex = mw.text.trim(tostring(row.color)):gsub("^#", "")
            if hex == "" then
                return nil
            end

            return "#" .. hex
        end

        local function formatIngredientName(ingredient, lang, ui)
            local isLiquid = ingredient["recipe_ingridient.is_liquid"]
            local specific = ingredient["recipe_ingridient.specific"]
            local specificId = ingredient["recipe_ingridient.specific_id"] or ""

            if specific and specificId ~= "" then
                local object

                if isLiquid then
                    object = Locale.getLiquid(specificId, lang)
                    local name = object and object.name or specificId

                    return name
                else
                    object = Locale.getItem(specificId, lang)
                    local name = object and object.name or specificId

                    return formatItemIconLink(specificId, name) .. " " .. name
                end
            end

            if ingredient["recipe_ingridient.is_liquid"] == true then
                return ui["recipe.any_liquid_with"] or "Any liquid with"
            end

            return ui["recipe.any_item_with"] or "Any item with "
        end

        local function formatIngredientDetail(ingredient, lang, ui)
            local strings = {}
            local specific = ingredient["recipe_ingridient.specific"]
            local specificId = ingredient["recipe_ingridient.specific_id"] or ""
            local isLiquid = ingredient["recipe_ingridient.is_liquid"] == true
            local quality = ingredient["recipe_ingridient.quality"]

            local entries = {}

            if type(quality) == "table" then
                entries = quality
            elseif type(quality) == "string" and quality ~= "" then
                entries = { quality }
            end

            if not specific or specificId == "" then
                if #entries > 0 then
                    local n, qualityId = qualityLabel(entries[1])

                    if qualityId then
                        local name = qualityDisplayName(qualityId, lang)

                        if isLiquid then
                            local template = ui["recipe.liquid_quality"] or "Total (%s) %s quality"

                            strings[#strings + 1] = "<span style='color: #aaaaaa;'>- "
                                .. string.format(template, formatNumber(n), name) .. "</span>"
                        else
                            local template = ui["recipe.quality"] or "(%s) %s quality"

                            strings[#strings + 1] = "<span style='color: #aaaaaa;'>- "
                                .. string.format(template, formatNumber(n), name) .. "</span>"
                        end
                    end
                end
            end

            local minCond = tonumber(ingredient["recipe_ingridient.minimum_condition"])

            if isLiquid then
                if minCond and minCond > 0 then
                    local template = ui["recipe.liquid_condition_at_least"] or "At least %s mL"

                    strings[#strings + 1] = "<span style='color: #aaaaaa;'>- "
                        .. string.format(template, formatNumber(minCond)) .. "</span>"
                end
            else
                if minCond and minCond > 0 then
                    local pct = math.floor(minCond * 100 + 0.5)
                    local template = ui["recipe.condition_at_least"] or "At least %d%% condition"

                    strings[#strings + 1] = "<span style='color: #aaaaaa;'>- "
                        .. string.format(template, pct) .. "</span>"
                end
            end

            if #strings == 0 then
                return nil
            end

            return table.concat(strings, "<br/>")
        end

        local function formatResultDetail(recipe, ui)
            local isLiquidResult = recipe["recipe_result.is_liquid"]

            local footLines = {
                {
                    key = isLiquidResult and "recipe.volume" or "recipe.condition",
                    value = recipe["recipe_result.result_condition"] * (isLiquidResult and 1 or 100),
                    fallback = isLiquidResult and "Volume: %dmL" or "Condition: %d%%"
                },
                { key = "recipe.amount",      value = recipe["recipe_result.amount"],           fallback = "Amount: %d" },
                { key = "recipe.intRequired", value = recipe["int"],                            fallback = "INT needed: %d" },
            }

            local parts = {}

            for _, line in ipairs(footLines) do
                local num = tonumber(line.value)
                if num then
                    local template = ui[line.key] or line.fallback

                    if line.key == "recipe.intRequired" then
                        parts[#parts + 1] = "<span style='color: #00ff00;'>" .. string.format(template, num) .. "</span>"
                    else
                        parts[#parts + 1] = "<span>" .. string.format(template, num) .. "</span>"
                    end
                end
            end

            return table.concat(parts, "<br/>")
        end

        function p.main(frame)
            local args = getArgs(frame)

            local recipeId = args.recipe_id or args[1] or args["1"]
            recipeId = recipeId and mw.text.trim(tostring(recipeId)) or ""

            if recipeId == "" then
                return "[[Category:Errors]]<strong>RecipeBucket:</strong> missing recipe id."
            end

            local result = p.fetchResultItem(recipeId)
            if not result then
                return "[[Category:Errors]]<strong>RecipeBucket:</strong> no Bucket row for '" .. recipeId .. "'."
            end

            local lang = Locale.resolveLang(frame)
            local ui = Locale.wikiUi(lang)

            local resultId = result["recipe_result.id"]
            local amount = result["recipe_result.amount"]
            local resultCondition = result["recipe_result.result_condition"]
            local isLiquid = result["recipe_result.is_liquid"]

            local object
            local count
            if isLiquid == true then
                object = Locale.getLiquid(resultId, lang)
                if resultCondition > 1 then
                    count = string.format(ui["recipe.countLiquid"] or "(%dmL)", resultCondition)
                end
            else
                object = Locale.getItem(resultId, lang)
                if amount > 1 then
                    count = string.format(ui["recipe.count"] or "(x%d)", amount)
                end
            end

            local name = object and object.name or resultId

            if count then
                name = name .. " " .. count
            end

            local ingredients = p.fetchIngridients(recipeId)

            local divContainer = mw.html.create('div')
                :addClass("cu-recipes")

            local headContainer = divContainer:tag("div"):addClass("cu-recipe-head")
            headContainer:tag("span")
                :addClass("cu-recipe-head-text")
                :wikitext(name)
            local headImage = headContainer:tag("div"):addClass("cu-recipe-head-image")

            local liquidColor = isLiquid == true and fetchLiquidColor(resultId) or nil
            if liquidColor then
                headImage:tag("div")
                    :addClass("cu-recipe-head-liquid")
                    :css("background-color", liquidColor)
                    :wikitext("&nbsp;")
            else
                headImage:wikitext("[[File:" .. resultId .. ".png|48x48px]]")
            end

            local bodyContainer = divContainer:tag("div"):addClass("cu-recipe-body")
            bodyContainer:tag("div")
                :addClass("cu-recipe-body-title")
                :wikitext(ui["recipe.ingredients"] or "Ingredients")

            local grouped = {}
            local order = {}

            for _, ingredient in ipairs(ingredients) do
                local nameHtml = formatIngredientName(ingredient, lang, ui)
                local detailHtml = formatIngredientDetail(ingredient, lang, ui)
                local key = nameHtml .. "\0" .. (detailHtml or "")

                local entry = grouped[key]
                if entry then
                    entry.count = entry.count + 1
                else
                    entry = { name = nameHtml, detail = detailHtml, count = 1 }
                    grouped[key] = entry
                    order[#order + 1] = key
                end
            end

            for _, key in ipairs(order) do
                local entry = grouped[key]
                local block = bodyContainer:tag("div"):addClass("cu-recipe-ingredient")

                local nameText = entry.name
                if entry.count > 1 then
                    nameText = nameText .. " " .. string.format(ui["recipe.count"] or "(x%d)", entry.count)
                end

                block:tag("div")
                    :addClass("cu-recipe-ingredient-name")
                    :wikitext(nameText)

                if entry.detail then
                    block:tag("div")
                        :addClass("cu-recipe-ingredient-detail")
                        :wikitext(entry.detail)
                end
            end

            local footerContainer = divContainer:tag("div"):addClass("cu-recipe-foot")

            footerContainer:tag("div")
                :addClass("cu-recipe-foot-title")
                :wikitext(ui["recipe.info"] or "Info")
            local block = footerContainer:tag("div"):addClass("cu-recipe-foot-attributes")

            local resultInfo = formatResultDetail(result, ui)
            if resultInfo then
                    block:tag("div")
                         :wikitext(resultInfo)
            end

            return tostring(divContainer)
        end

        return p
        """;

    public const string RouterRecipeModule =
        """
        -- Module:RecipeData
        -- Routes language-neutral recipe rows into Bucket tables.
        
        local p = {}

        local function putRow(r)
            bucket("recipe").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:Recipe/data")
            local count = 0
            for _, row in ipairs(data) do
                putRow(row)
                count = count + 1
            end
            return string.format("Stored %d recipes into Bucket.", count)
        end

        return p
        """;

    public const string RouterRecipeItemModule =
        """
        -- Module:RecipeItemData
        -- Routes language-neutral recipe rows into Bucket tables.
        
        local p = {}

        local function putRow(r)
            bucket("recipe_ingridient").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:RecipeItem/data")
            local count = 0
            for _, row in ipairs(data) do
                putRow(row)
                count = count + 1
            end
            return string.format("Stored %d recipe items into Bucket.", count)
        end

        return p
        """;

    public const string RouterRecipeResultModule =
        """
        -- Module:RecipeResultData
        -- Routes language-neutral recipe rows into Bucket tables.
        
        local p = {}

        local function putRow(r)
            bucket("recipe_result").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:RecipeResult/data")
            local count = 0
            for _, row in ipairs(data) do
                putRow(row)
                count = count + 1
            end
            return string.format("Stored %d recipe results into Bucket.", count)
        end

        return p
        """;

    #endregion Recipes

    #region Moodles

    /// <summary>
    /// Reads moodle rows from Bucket + locale strings and renders a wikitable.
    /// <c>table</c> accepts multiple positional ids in one table; <c>singleRowTable</c> is an alias.
    /// Per-row intensity: <c>hypotension3:2</c> or named <c>intensity2=2</c>; global <c>intensity=</c> is fallback.
    /// Template:MoodleTable: {{MoodleTable|palpitations|hypotension3:2|pain1}}.
    /// Optional: <c>collapse=1</c> (mw-collapsible), <c>collapsed=1</c> (start collapsed), <c>caption=…</c> (|+ row).
    /// Icon stack + critical bounce/flash: classes in MediaWiki:Gadget-moodle.css (gadget "moodle").
    /// Icon display size comes from Bucket <c>icon_src_size</c> (20px sprites → 48px, 30px → 64px).
    /// Keep in sync with the live wiki module.
    /// </summary>
    public const string MoodleBucketModule =
        """
        local Locale = require("Module:Locale")
        local getArgs = require("Module:Arguments").getArgs
        local tmpRenderer = require("Module:TMPRender")

        local templateYes = mw.getCurrentFrame():expandTemplate{ title = "Yes" }
        local templateNo = mw.getCurrentFrame():expandTemplate{ title = "No" }

        local MOODLE_ICON_SRC_BASE = 20
        local MOODLE_ICON_SRC_LARGE = 30
        local MOODLE_ICON_DISPLAY_BASE = 48
        local MOODLE_ICON_DISPLAY_LARGE = 64

        local p = {}

        local function firstRow(result)
            return result and result[1] or nil
        end

        local TABLE_COLUMNS = {
            '! scope="col" style="width: 10%" | Moodle',
            '! scope="col" style="width: 50%" | Description',
            '! scope="col" style="width: 5%" | [[Unchipped mode|Unchipped]]',
            '! scope="col" | Cause',
        }

        local function toBoolean(v)
            if v == true then return true end
            if v == false or v == nil then return false end

            v = tostring(v):lower()

            return v == "true" or v == "1" or v == "yes"
        end

        ---Returns the longest length of any word in sentence `str`.
        local function maxWordLen(str)
            local maxLen = 0
            for _, word in ipairs(mw.text.split(str, " ", true)) do
                local len = string.len(word)
                maxLen = len > maxLen and len or maxLen
            end
            return maxLen
        end

        local function buildTableCaption(args)
            local cap = args.caption
            if cap ~= nil and tostring(cap) ~= "" then
                return "|+ " .. tostring(cap)
            end
            return "|+"
        end

        local function resolveIntensity(entry, args)
            local named = args["intensity" .. entry.index]
            if named ~= nil and tostring(named) ~= "" then
                return tostring(named)
            end
            if entry.intensity ~= nil and tostring(entry.intensity) ~= "" then
                return tostring(entry.intensity)
            end
            if args.intensity ~= nil and tostring(args.intensity) ~= "" then
                return tostring(args.intensity)
            end
            return nil
        end

        local function parseEntry(raw)
            raw = mw.text.trim(tostring(raw))
            if raw == "" then
                return nil
            end

            local colon = raw:find(":", 1, true)
            if not colon then
                return { id = raw, intensity = nil }
            end

            local id = mw.text.trim(raw:sub(1, colon - 1))
            local intensity = mw.text.trim(raw:sub(colon + 1))
            if id == "" then
                return nil
            end

            return {
                id = id,
                intensity = intensity ~= "" and intensity or nil,
            }
        end

        local function collectEntries(args)
            local entries = {}
            for key, value in pairs(args) do
                if type(key) == "number" and value ~= nil and value ~= "" then
                    local parsed = parseEntry(value)
                    if parsed then
                        parsed.index = key
                        entries[#entries + 1] = parsed
                    end
                end
            end
            table.sort(entries, function(a, b) return a.index < b.index end)
            return entries
        end

        local MOOD_BACKGROUNDS = {
            [0] = "MoodleMood1.png",
            [1] = "MoodleMood2.png",
            [2] = "MoodleMood3.png",
            [3] = "MoodleMood4.png",
            [4] = "MoodleMood5.png",
            [5] = "MoodleMood6.png",
            [6] = "MoodleMood7.png",
            [7] = "MoodleMood8.png",
        }

        local function moodBackgroundFile(intensity)
            local n = tonumber(intensity)
            if n == nil then
                return "MoodleMood1.png"
            end
            if n >= 8 then
                return "MoodleMoodLast.png"
            end
            if n < 0 then
                return "MoodleMood1.png"
            end
            return MOOD_BACKGROUNDS[n] or "MoodleMood1.png"
        end

        local function resolveIconFilename(row, id)
            local icon = row.icon or id or "Deceased"
            if not icon:find("%.") then
                icon = "Moodle" .. mw.language.getContentLanguage():ucfirst(icon) .. ".png"
            end
            return icon
        end

        local function rowIconSrcSize(row)
            local n = tonumber(row and row.icon_src_size)
            if n and n > 0 then
                return n
            end
            return MOODLE_ICON_SRC_BASE
        end

        --- Maps native moodle sprite size (px) to wiki display size. 20→48, 30→64; linear between.
        local function moodleDisplaySize(srcW, srcH)
            local src = math.max(tonumber(srcW) or MOODLE_ICON_SRC_BASE, tonumber(srcH) or MOODLE_ICON_SRC_BASE)

            if src <= MOODLE_ICON_SRC_BASE then
                return MOODLE_ICON_DISPLAY_BASE
            end

            if src >= MOODLE_ICON_SRC_LARGE then
                return MOODLE_ICON_DISPLAY_LARGE
            end

            local t = (src - MOODLE_ICON_SRC_BASE) / (MOODLE_ICON_SRC_LARGE - MOODLE_ICON_SRC_BASE)

            return math.floor(MOODLE_ICON_DISPLAY_BASE + t * (MOODLE_ICON_DISPLAY_LARGE - MOODLE_ICON_DISPLAY_BASE) + 0.5)
        end

        local function fileThumb(frame, filename, srcSize)
            local display = moodleDisplaySize(srcSize, srcSize)
            return frame:preprocess(string.format("[[File:%s|%dx%dpx]]", filename, display, display))
        end

        local function iconFile(frame, row, id)
            local bg = moodBackgroundFile(row.intensity)
            local fg = resolveIconFilename(row, id)

            local fgSize = rowIconSrcSize(row)
            local bgSize = MOODLE_ICON_SRC_BASE
            local stackSize = moodleDisplaySize(math.max(fgSize, bgSize), math.max(fgSize, bgSize))
            local stackClass = "cu-moodle-icon-stack"

            if stackSize > MOODLE_ICON_DISPLAY_BASE then
                stackClass = stackClass .. " cu-moodle-icon-stack--lg"
            end

            local parts = {
                '<div class="' .. stackClass .. '" style="width:' .. stackSize .. "px;height:" .. stackSize .. 'px">',
                '<div class="cu-moodle-bg">' .. fileThumb(frame, bg, bgSize) .. "</div>",
                '<div class="cu-moodle-fg">' .. fileThumb(frame, fg, fgSize) .. "</div>",
            }

            if row.critical then
                parts[#parts + 1] = '<div class="cu-moodle-flash-overlay" aria-hidden="true"></div>'
            end

            parts[#parts + 1] = "</div>"

            return table.concat(parts, "")
        end

        local function moodleWidget(frame, row, id, nameHtml)
            local widgetClass = "cu-moodle-widget"
            if row.critical then
                widgetClass = widgetClass .. " cu-moodle-widget--critical"
            end
            return '<div class="' .. widgetClass .. '">'
                .. iconFile(frame, row, id)
                .. nameHtml
                .. "</div>"
        end

        local function unchippedCell(row)
            if row.chipped_only then
                return "[[File:Icon_cross_red.png|8x8px]]"
            end
            return "[[File:Icon_checkmark_green.png|8x8px]]"
        end

        local function fetchGameFieldValue(fieldId)
            if not fieldId or fieldId == "" then return nil end
            local row = firstRow(bucket("gamefield")
                .select("value")
                .where("game_field_id", fieldId)
                .run())
            return row and tonumber(row.value)
        end

        local function fetchBodyField(bodyFieldId)
            if not bodyFieldId or bodyFieldId == "" then return nil end
            return firstRow(bucket("bodyfield")
                .select("body_field_id", "label", "kind",
                        "heal_speed_field_id", "intensity_scale_field_id", "splint_multiplier_field_id")
                .where("body_field_id", bodyFieldId)
                .run())
        end

        local function formatDuration(seconds)
            seconds = math.max(0, math.floor(seconds + 0.5))
            local mins = math.floor(seconds / 60)
            local secs = seconds % 60
            if mins > 0 and secs > 0 then
                return mins .. " min " .. secs .. " s"
            elseif mins > 0 then
                return mins .. " min"
            end
            return secs .. " s"
        end

        local function timerThresholdForIntensity(intensity, scale)
            if intensity <= 0 then return 0 end
            return (intensity - 0.5) / scale
        end

        local function formatTimerIntensity(bodyFieldId, lang)
            local meta = fetchBodyField(bodyFieldId)
            if not meta or meta.kind ~= "timer" then
                return Locale.bodyField(bodyFieldId, lang)
            end

            local label = meta.label or Locale.bodyField(bodyFieldId, lang)
            local healSpeed = fetchGameFieldValue(meta.heal_speed_field_id)
            local intensityScale = fetchGameFieldValue(meta.intensity_scale_field_id)
            local splintMultiplier = fetchGameFieldValue(meta.splint_multiplier_field_id)

            if not healSpeed or not intensityScale or not splintMultiplier then
                return label
            end

            local ui = Locale.wikiUi(lang)
            local intensityWord = ui.intensity_label or "Intensity"

            local thresholds = {}
            for intensity = 3, 1, -1 do
                thresholds[intensity] = timerThresholdForIntensity(intensity, intensityScale)
            end

            local lines = {}
            for intensity = 3, 0, -1 do
                local text
                if intensity == 3 then
                    local seconds = thresholds[3] / healSpeed
                    local splintSeconds = thresholds[3] / (healSpeed * splintMultiplier)
                    text = label .. " > " .. formatDuration(seconds)
                        .. " (~" .. formatDuration(splintSeconds) .. " with splint)"
                elseif intensity == 0 then
                    text = label .. " ≤ " .. formatDuration(thresholds[1] / healSpeed)
                else
                    text = label .. " " .. formatDuration(thresholds[intensity] / healSpeed)
                        .. " – " .. formatDuration(thresholds[intensity + 1] / healSpeed)
                end
                lines[#lines + 1] = intensityWord .. " " .. intensity .. ": " .. text
            end

            return table.concat(lines, "<br />")
        end

        local function formatCause(row, lang)
            local parts = {}

            if row.precondition_display and row.precondition_display ~= "" then
                parts[#parts + 1] = row.precondition_display
            end

            if row.intensity_body_field_id and row.intensity_body_field_id ~= "" then
                parts[#parts + 1] = formatTimerIntensity(row.intensity_body_field_id, lang)
            end

            if #parts == 0 then
                return "—"
            end
            return table.concat(parts, "<br />")
        end
        
        function p.fetch(localeId, intensity)
            local results = bucket("moodle")
                .select(
                    "locale_id", "icon", "icon_src_size", "desc_locale_key", "precondition_for_moodle",
                    "precondition_display", "intensity", "intensity_body_field_id",
                    "critical", "critical_expr", "chipped_only")
                .where("locale_id", localeId)
                .run()

            if not results or #results == 0 then
                return nil
            end

            if intensity ~= nil and intensity ~= "" then
                local want = tonumber(intensity)
                if want then
                    for _, row in ipairs(results) do
                        if tonumber(row.intensity) == want then
                            return row
                        end
                    end
                    return nil
                end
            end

            return results[1]
        end

        --- Extract arguments for use in the table function.
        local function extractArgsForTableFn(frame)
            local args = getArgs(frame, { trim = true, removeBlanks = false })
            local entries = collectEntries(args)

            if #entries == 0 then
                local fallback = args.id or args.locale_id
                local parsed = fallback and parseEntry(fallback) or nil
                if parsed then
                    parsed.index = 1
                    entries = { parsed }
                end
            end

            if #entries == 0 then
                return '[[Category:MoodleRowError]]<strong>MoodleBucket:</strong> missing locale id.'
            end

            return args, entries
        end

        function p.interactiveTable(frame)
            local args, entries = extractArgsForTableFn(frame)
            if type(args) ~= "table" then
                return args
            end

            local lang = Locale.resolveLang(frame)
            local ui = Locale.wikiUi(lang)

            local tbl = mw.html.create('table')
                :addClass("wikitable")
                :addClass("moodles-table")
            local headRow = tbl:tag("tr")
                :addClass("moodles-table-moodles-row")
            local descRow = tbl:tag("tr")
                :addClass("moodles-table-desc-row")

            for idx, entry in ipairs(entries) do
                local intensity = resolveIntensity(entry, args)
                local row = p.fetch(entry.id, intensity)
                if not row then
                    error("MoodleBucket: no Bucket row for " .. mw.text.nowiki(entry.id))
                end

                local locRow = Locale.getMoodle(entry.id, lang)
                if not locRow then
                    locRow = { name = entry.id, description = "" }
                end

                local isCritical = row.critical or (row.critical_expr and row.critical_expr ~= "")
                local nameClass = isCritical and "cu-moodle-name cu-moodle-name--critical" or "cu-moodle-name"
                local nameNowiki = mw.text.nowiki(locRow.name or "")
                local nameHtml = mw.html.create("span")
                    :addClass(nameClass)
                    -- used for overflow prevention in css
                    :css("--max-word-len", maxWordLen(nameNowiki))
                    :wikitext(nameNowiki)

                local moodleEl = headRow:tag("th")
                    :wikitext(moodleWidget(frame, row, entry.id, tostring(nameHtml)))

                if idx == 1 then moodleEl:addClass("selected") end

                -- todo: localize tooltip
                local requiresChipPart = frame:expandTemplate{ title = "Tooltip", args = { (ui.is_chipped or "Requires chip"), "Whether the moodle is only visible when the chip is functional." } }

                descRow:tag("td")
                    :wikitext("<p style='color: var(--text-subtle);'>" .. requiresChipPart .. " " .. (row.chipped_only and templateYes or templateNo) .. "</p>")
                    :wikitext(tmpRenderer.render_tmp_text(locRow.description or ""))
                    :wikitext("<p><span style='color: var(--text-subtle);'>" .. (ui.caused_by or "Caused by") .. "</span><br>" .. formatCause(row, lang) .. "</p>")
            end

            return tbl
        end
        
        return p
        """;

    public const string RouterMoodleModule =
        """
        -- Module:MoodleData
        -- Routes language-neutral moodle rows into Bucket tables.
        -- Localized names/descriptions are resolved at render time via Module:Locale.

        local templateTable = require("Module:MoodleBucket")

        local p = {}

        local function putRow(r)
            bucket("moodle").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:Moodle/data")
            local count = 0
            for _, row in ipairs(data) do
                putRow(row)
                count = count + 1
            end
            return string.format("Stored %d moodles into Bucket.", count)
        end

        return p
        """;

    #endregion Moodles

    #region Buildings
    
    // TODO: Actually fill this with stuff
    public const string BuildingBucketModule =
        """
        local Locale = require("Module:Locale")
        local getArgs = require("Module:Arguments").getArgs
        local yesNo = require("Module:Yesno")

        local p = {}

        local function firstRow(result)
            return result and result[1] or nil
        end

        local function paramValue(v)
            if v == nil then return "" end
            if type(v) == "boolean" then
                return yesNo(v) and "true" or "false"
            end
            if type(v) == "table" then
                return table.concat(v, ", ")
            end
            return tostring(v)
        end

        function p.fetch(buildingId)
            return firstRow(bucket("building")
                .select("building_id", "items_drop_on_destroy", "health", "require_ground",
                        "skip_description_set", "drop_chance_multiplier", "guaranteed_drop_amount",
                        "always_drop", "item_categories_to_add", "block_footstep_sound_id",
                        "cant_hit", "animal", "ignore_body_optimize", "metallic")
                .where("building_id", buildingId)
                .run())
        end

        -- =================================================
        function p.infobox(frame)
            local args = getArgs(frame)

            local buildingId = args.building_id or args[1] or args["1"]
            buildingId = buildingId and mw.text.trim(tostring(buildingId)) or ""
            if buildingId == "" then
                return "[[Category:Errors]]<strong>BuildingBucket:</strong> missing building id."
            end

            local row = p.fetch(buildingId)
            if not row then
                return "[[Category:Errors]]<strong>BuildingBucket:</strong> no Bucket row for '" .. buildingId .. "'."
            end

            local lang = Locale.resolveLang(frame)
            local localeBuilding = Locale.getBuilding(buildingId, lang)

            if DEBUG then 
                mw.log("> lang")
                mw.logObject(lang)
                mw.log("> localeBuilding")
                mw.logObject(localeBuilding)
                mw.log("> args")
                mw.logObject(args)
                mw.log("> row")
                mw.logObject(row)
            end

            local resArgs = {
                building_id = buildingId,
                display_name = localeBuilding and localeBuilding.name or buildingId,
                description = (localeBuilding and not row.skip_description_set) and localeBuilding.description or "",
            }

            for key, value in pairs(row) do
                resArgs[key] = paramValue(value)
            end

            local hex = row.color and mw.text.trim(tostring(row.color)) or ""
            hex = hex:gsub("^#", "")
            if hex ~= "" then
                resArgs.color_css = hex
            end

            return frame:expandTemplate{ title = "Building Infobox", args = resArgs }
        end

        -- Renders N infoboxes. For debugging purposes.
        function p.n_infoboxes(frame)
            local n = tonumber(frame.args.n) or 1
            local from = tonumber(frame.args.from) or 1

            local parent = mw.html.create("div")
                :css("display", "flex")
                :css("flex-direction", "row")
                :css("flex-wrap", "wrap")

            local queryRes = bucket("building")
                .select("building_id")
                .run()

            local total = #queryRes

            local count = 0
            for _, obj in ipairs(queryRes) do
                count = count + 1
                if count >= from then
                    local buildingId = obj.building_id
                    frame.args[1] = buildingId
                    parent:node(p.infobox(frame))
                end

                if count == (from + n) then break end
            end

            return "Displaying " .. from .. " to " .. count .. " of " .. total, parent
        end

        return p
        """;
    
    public const string RouterBuildingModule =
        """
        -- Module:BuildingData
        -- Routes language-neutral building rows into Bucket tables.
        -- Localized names/descriptions are resolved at render time via Module:Locale.

        local templateTable = require("Module:BuildingBucket")

        local p = {}

        local function putRow(r)
            bucket("building").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:Building/data")
            local count = 0
            for _, row in ipairs(data) do
                putRow(row)
                count = count + 1
            end
            return string.format("Stored %d buildings into Bucket.", count)
        end

        return p
        """;
    
    #endregion
    
    #region Game Fields

    public const string RouterGameFieldModule =
        """
        -- Module:GameFieldData
        -- Routes dumped game scalar constants into Bucket tables.

        local p = {}

        local function putRow(r)
            bucket("gamefield").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:GameField/data")
            for _, row in ipairs(data) do
                putRow(row)
            end
            return string.format("Stored %d game fields into Bucket.", #data)
        end

        return p
        """;

    public const string RouterBodyFieldModule =
        """
        -- Module:BodyFieldData
        -- Routes body field metadata into Bucket tables.

        local p = {}

        local function putRow(r)
            bucket("bodyfield").put(r)
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:BodyField/data")
            for _, row in ipairs(data) do
                putRow(row)
            end
            return string.format("Stored %d body fields into Bucket.", #data)
        end

        return p
        """;

    #endregion Game Fields

    public const string TriggerItemPage =
        """
        This page stores all item data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:ItemData|putAll}}
        """;

    public const string TriggerLiquidPage =
        """
        This page stores all liquid data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:LiquidData|putAll}}
        """;

    public const string TriggerBlockPage =
    """
        This page stores all block data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:BlockData|putAll}}
        """;

    public const string TriggerRecipePage =
        """
        This page stores all liquid data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:RecipeData|putAll}}
        """;

    public const string TriggerRecipeItemPage =
        """
        This page stores all liquid data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:RecipeItemData|putAll}}
        """;

    public const string TriggerRecipeResultPage =
        """
        This page stores all liquid data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:RecipeResultData|putAll}}
        """;

    public const string TriggerMoodlePage =
        """
        This page stores all moodle data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:MoodleData|putAll}}
        """;

    public const string TriggerBuildingPage =
        """
        This page stores all moodle data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:BuildingData|putAll}}
        """;

    public const string TriggerGameFieldPage =
        """
        This page stores all game field constants into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:GameFieldData|putAll}}
        """;

    public const string TriggerBodyFieldPage =
        """
        This page stores all body field metadata into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:BodyFieldData|putAll}}
        """;
}

/*
TMPRenderer

local p = {}

function p.render_tmp_text_page(frame)
    local raw_text = frame.args['raw_text']
    
    if raw_text == nil then return '!!!ERROR!!!' end
    
    return p.render_tmp_text(raw_text)
end
    
function p.render_tmp_text(raw_text)
    local root = mw.html.create('span')
    
    local foreground = nil
    local bold = false
    local italic = false
    local font_size_pct = 100
    
    current_index = 1
    
    while current_index <= #raw_text do
        local open_bracket_index = string.find(raw_text, '<', current_index, true)
        local closing_bracket_index = nil
        
        if open_bracket_index ~= nil then
            closing_bracket_index = string.find(raw_text, '>', open_bracket_index + 1, true)
        end
        
        local found_tag = closing_bracket_index ~= nil
        local run_length = (found_tag and open_bracket_index or (#raw_text + 1)) - current_index
        
        if run_length > 0 then
            local sub_text = string.sub(raw_text, current_index, current_index + run_length - 1)
            local run = root:tag('span'):wikitext(sub_text)
            if bold then run = run:css('font-weight', 'bold') end
            if italic then run = run:css('font-style', 'italic') end
            if font_size_pct and font_size_pct ~= 100 then run = run:css('font-size', tostring(font_size_pct / 100) .. 'em') end
            if foreground then run = run:css('color', foreground) end
        end
        
        current_index = current_index + run_length
        
        if found_tag then
            local tag_contents_length = closing_bracket_index - open_bracket_index - 1
            local tag = string.sub(raw_text, open_bracket_index + 1, open_bracket_index + tag_contents_length)
            
            local should_print_raw = false
            
            if tag == 'b' then
                bold = true
            elseif tag == '/b' then
                bold = false
            elseif tag == 'i' then
                italic = true
            elseif tag == '/i' then
                italic = false
            elseif tag == '/color' then
                foreground = nil
            else
                should_print_raw = true
            end
            
            if should_print_raw then
                local _, _, size = string.find(tag, '^size=([0-9]*)%%$')
                if size ~= nil and tonumber(size) ~= nil then
                    font_size_pct = tonumber(size)
                    should_print_raw = false
                end
            end
            
            if should_print_raw then
                local _, _, color = string.find(tag, '^color="([a-z]*)"$')
                if color ~= nil then
                    foreground = color
                    should_print_raw = false
                end
            end
            
            if should_print_raw then
                local _, _, color = string.find(tag, '^color=#([0-9a-fA-F]*)$')
                if color ~= nil and (#color == 6 or #color == 8) then
                    foreground = '#' .. color
                    should_print_raw = false
                end
            end
            
            if should_print_raw then
                local sub_text = string.sub(raw_text, open_bracket_index, open_bracket_index + tag_contents_length + 1)
                local run = root:tag('span'):wikitext(sub_text)
                if bold then run = run:css('font-weight', 'bold') end
                if italic then run = run:css('font-style', 'italic') end
                if font_size_pct and font_size_pct ~= 100 then run = run:css('font-size', tostring(font_size_pct / 100) .. 'em') end
                if foreground then run = run:css('color', foreground) end
            end
            
            current_index = current_index + tag_contents_length + 2
        end
    end
    
    local final_text, _ = string.gsub(tostring(root), '\n', '<br />')
    return final_text
end

return p
*/