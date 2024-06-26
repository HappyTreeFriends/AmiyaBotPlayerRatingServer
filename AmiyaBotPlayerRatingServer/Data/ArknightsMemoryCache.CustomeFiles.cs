using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Json.Path;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text;
using System.Text.RegularExpressions;

namespace AmiyaBotPlayerRatingServer.Data
{
    public partial class ArknightsMemoryCache
    {
        private void GenerateCustomFiles()
        {
            try
            {
                GenerateArknightsConfig();
                GenerateCharacterNames();
                GenerateCharacterTableFull();
                GenerateOperatorArchiveTable();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateCustomFiles failed");
            }

            _logger.LogInformation("Custom files generated");
        }

        private void GenerateCharacterTableFull()
        {
            try
            {
                //进行一点点逻辑处理
                var characterTable = JsonConvert.DeserializeObject<JToken>(GetText("character_table.json")!) as JObject;
                var characterNames = GetObject<Dictionary<String, String>>("character_names.json");

                if (characterTable == null||characterNames==null)
                {
                    return;
                }
                
                var newCharacterTable = new JObject();

                foreach (var character in characterTable)
                {
                    if (character.Value == null)
                    {
                        continue;
                    }

                    if (characterNames.Keys.Contains(character.Key))
                    {
                        character.Value["charId"] = character.Key;
                        newCharacterTable[character.Key] = character.Value;
                    }
                }
                
                characterTable = newCharacterTable;

                var skillTable = JsonConvert.DeserializeObject<JToken>(GetText("skill_table.json")!) as JObject;
                var skillDict = new Dictionary<String, JToken>();
                foreach (var skillObj in skillTable!)
                {
                    var key = skillObj.Key;
                    var value = skillObj.Value!;
                    skillDict.Add(key, value);
                }

                foreach (var character in characterTable)
                {
                    var value = character.Value;
                    var skills = value!["skills"] as JArray;
                    if (skills == null) continue;
                    foreach (var skill in skills)
                    {
                        var skillId = skill["skillId"]?.ToString();
                        if (skillId == null)
                        {
                            continue;
                        }
                        var skillData = skillDict.GetValueOrDefault(skillId);
                        if (skillData != null)
                        {
                            skill["skillData"] = skillData;
                        }
                    }
                }

                var skinTable = JsonConvert.DeserializeObject<JToken>(GetText("skin_table.json")!) as JObject;
                foreach (var skinObj in (skinTable!["charSkins"] as JObject)!)
                {
                    var value = skinObj.Value!;
                    //value!["skinId"] = key; 该对象已有skinId
                    var charId = value["charId"]?.ToString()!;

                    var character = characterTable[charId];
                    if (character != null)
                    {
                        var skin = character["skins"] as JArray;
                        if (skin == null)
                        {
                            skin = new JArray();
                            character["skins"] = skin;
                        }
                        skin.Add(value);
                    }
                }

                LoadJson(characterTable, "character_table_full.json");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void GenerateCharacterNames()
        {
            //进行一点点逻辑处理
            var characterTable = JsonConvert.DeserializeObject<JToken>(GetText("character_table.json")!) as JObject;

            if (characterTable == null)
            {
                return;
            }

            var characterNames = new Dictionary<String,String>();

            foreach (var character in characterTable)
            {
                if (character.Value == null)
                {
                    continue;
                }

                var obtain = character.Value["itemObtainApproach"]?.ToString();
                if (String.IsNullOrWhiteSpace(obtain) ||
                    (!obtain.Contains("凭证交易所") && !obtain.Contains("招募寻访") &&
                    !obtain.Contains("活动获得") && !obtain.Contains("主线剧情") &&
                    !obtain.Contains("信用交易所") && !obtain.Contains("限时礼包") &&
                    !obtain.Contains("周年奖励"))
                    )
                {
                    continue;
                }
                character.Value["charId"] = character.Key;
                characterNames[character.Key] = character.Value["name"]?.ToString()??"";
            }

            LoadObject(characterNames, "character_names.json");
        }

        private void GenerateArknightsConfig()
        {
            /*
             config = {
                   'classes': {
                       'CASTER': '术师',
                       'MEDIC': '医疗',
                       'PIONEER': '先锋',
                       'SNIPER': '狙击',
                       'SPECIAL': '特种',
                       'SUPPORT': '辅助',
                       'TANK': '重装',
                       'WARRIOR': '近卫',
                   },
                   'token_classes': {'TOKEN': '召唤物', 'TRAP': '装置'},
                   'high_star': {'5': '资深干员', '6': '高级资深干员'},
                   'types': {'ALL': '不限部署位', 'MELEE': '近战位', 'RANGED': '远程位'},
               }
             */

            var classesConfig = new Dictionary<string, string>
            {
                {"CASTER", "术师"},
                {"MEDIC", "医疗"},
                {"PIONEER", "先锋"},
                {"SNIPER", "狙击"},
                {"SPECIAL", "特种"},
                {"SUPPORT", "辅助"},
                {"TANK", "重装"},
                {"WARRIOR", "近卫"},
            };
            LoadObject(classesConfig, "classes.json");

            var tokenClassesConfig = new Dictionary<string, string>
            {
                {"TOKEN", "召唤物"},
                {"TRAP", "装置"},
            };
            LoadObject(tokenClassesConfig, "token_classes.json");

            var highStarConfig = new Dictionary<string, string>
            {
                {"5", "资深干员"},
                {"6", "高级资深干员"},
            };
            LoadObject(highStarConfig, "high_star.json");

            var typesConfig = new Dictionary<string, string>
            {
                {"ALL", "不限部署位"},
                {"MELEE", "近战位"},
                {"RANGED", "远程位"},
            };
            LoadObject(typesConfig, "types.json");


        }

        private string RemovePunctuation(string text)
        {
            string punctuationCn = "，。、！？；：“”‘’（）【】《》——…";
            string punctuationEn = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

            foreach (char c in punctuationCn)
            {
                text = text.Replace(c.ToString(), "");
            }

            foreach (char c in punctuationEn)
            {
                text = text.Replace(c.ToString(), "");
            }

            return text;
        }

        public string ParseTemplate(JToken blackboard, string description)
        {
            var formatter = new Dictionary<string, Func<double, string>>
            {
                { "0%", v => $"{Math.Round(v * 100)}%" }
            };

            var dataDict = new Dictionary<string, object>();
            foreach (var item in blackboard)
            {
                var key = item["key"].ToString();
                var valueStr = item["valueStr"]?.ToString();
                var value = item["value"]?.ToString();
                dataDict[key] = valueStr ?? value;
            }

            var desc = HtmlTagFormat(description.Replace(">-{", ">{"));
            var formatStr = Regex.Matches(desc, @"({(\S+?)})");

            foreach (Match descItem in formatStr)
            {
                var key = descItem.Groups[2].Value.Split(':');
                var fd = key[0].ToLower().Trim('-');
                if (dataDict.ContainsKey(fd))
                {
                    var value = Integer(dataDict[fd]);

                    if (key.Length >= 2 && formatter.ContainsKey(key[1]) && value != null)
                    {
                        value = Integer(formatter[key[1]](value.Value));
                    }

                    desc = desc.Replace(descItem.Groups[1].Value, $" [cl {value}@#174CC6 cle] ");
                }
            }

            return desc;
        }

        public int? Integer(object value)
        {
            if (value == null) return null;
            if (int.TryParse(value.ToString(), out int result))
            {
                return result;
            }
            return null;
        }


        public string HtmlTagFormat(string text)
        {
            var htmlSymbol = new Dictionary<string, string>
            {
                { "&lt;", "<" },
                { "&gt;", ">" },
                { "&amp;", "&" },
                { "&quot;", "\"" },
                { "&apos;", "'" }
                // Add more HTML symbols and their replacements as needed
            };

            foreach (var symbol in htmlSymbol)
            {
                text = text.Replace(symbol.Key, symbol.Value);
            }

            // Remove XML tags using a simple regex pattern
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            return text;
        }


        private string BuildRange(JArray grids)
        {
            int[] _max = { 0, 0, 0, 0 };

            var items = new List<JObject> { JObject.FromObject(new { row = 0, col = 0 }) };
            items.AddRange(grids.Select(g => (JObject)g));

            foreach (var item in items)
            {
                int row = item["row"].ToObject<int>();
                int col = item["col"].ToObject<int>();
                if (row <= _max[0]) _max[0] = row;
                if (row >= _max[1]) _max[1] = row;
                if (col <= _max[2]) _max[2] = col;
                if (col >= _max[3]) _max[3] = col;
            }

            int width = Math.Abs(_max[2]) + _max[3] + 1;
            int height = Math.Abs(_max[0]) + _max[1] + 1;

            string empty = "　";
            string block = "□";
            string origin = "■";

            var rangeMap = new List<List<string>>();
            for (int h = 0; h < height; h++)
            {
                var row = new List<string>();
                for (int w = 0; w < width; w++)
                {
                    row.Add(empty);
                }
                rangeMap.Add(row);
            }

            foreach (var item in grids)
            {
                int x = Math.Abs(_max[0]) + item["row"].ToObject<int>();
                int y = Math.Abs(_max[2]) + item["col"].ToObject<int>();
                rangeMap[x][y] = block;
            }
            rangeMap[Math.Abs(_max[0])][Math.Abs(_max[2])] = origin;

            var result = new StringBuilder();
            foreach (var row in rangeMap)
            {
                result.AppendLine(string.Join(string.Empty, row));
            }

            return result.ToString();
        }


        private void GenerateOperatorArchiveTable()
        {
            _logger.LogInformation("start generating operator archive!");

            var characterNames = GetObject<Dictionary<String, String>>("character_names.json");
            var characterTable = GetJson("character_table.json") as JObject;
            var teamTable = GetJson("handbook_team_table.json");
            var subClassesTable = GetJson("uniequip_table.json")?["subProfDict"];
            var itemTable = GetJson("item_table.json")?["items"];
            var handbookInfoTable = GetJson("handbook_info_table.json")?["handbookDict"];

            var wordTable = GetJson("charword_table.json");
            var voiceLangDict = wordTable?["voiceLangDict"];
            var voiceLangTypeDict = wordTable?["voiceLangTypeDict"];

            var skinTable = GetJson("skin_table.json")?["charSkins"];
            var skillTable = GetJson("skill_table.json") as JObject;
            var rangeTable = GetJson("range_table.json") as JObject;

            _logger.LogInformation("all resources loadded into memory!");

            if (characterNames == null
                || characterTable == null
                || teamTable == null
                || subClassesTable == null
                || itemTable == null
                || handbookInfoTable == null
                || wordTable == null
                || voiceLangDict == null
                || voiceLangTypeDict == null)
            {
                return;
            }

            var operatorArchive = new JObject();

            foreach (var operatorId in characterNames.Keys)
            {
                var operatorJson = characterTable[operatorId];
                var operatorName = characterNames[operatorId];

                if (operatorJson==null) continue;

#pragma warning disable IDE0028 // 简化集合初始化
                var operatorArchiveData = new JObject();
#pragma warning restore IDE0028 // 简化集合初始化

                operatorArchiveData["id"] = operatorId;
                operatorArchiveData["cv"] = new JArray();

                if (operatorJson["position"] == null)
                {
                    _logger.LogInformation("Operator {0} {1} has no position", operatorId, operatorName);
                }
                operatorArchiveData["type"] = GetJson("types.json")?[operatorJson["position"]?.ToString()??""];
                operatorArchiveData["tags"] = new JArray();
                operatorArchiveData["range"] = "无范围";
                operatorArchiveData["rarity"] = operatorJson["rarity"]?.Type == JTokenType.String ? int.Parse(operatorJson["rarity"]?.ToString().Split('_').Last()!) : (operatorJson["rarity"]?.ToObject<int>() + 1);
                operatorArchiveData["number"] = operatorJson["displayNumber"];

                operatorArchiveData["name"] = characterNames[operatorId];
                operatorArchiveData["enName"] = operatorJson["appellation"];
                operatorArchiveData["wiki_name"] = operatorJson["name"];
                operatorArchiveData["index_name"] = RemovePunctuation(operatorJson["name"]?.ToString()!);
                operatorArchiveData["origin_name"] ="未知";

                operatorArchiveData["classes"] = GetJson("classes.json") ? [operatorJson["profession"]?.ToString()??""];
                operatorArchiveData["classes_sub"] =
                    subClassesTable[operatorJson["subProfessionId"]?.ToString()??""]?["subProfessionName"]??"";
                operatorArchiveData["classes_code"] = operatorJson["profession"];

                operatorArchiveData["sex"] = "未知";
                operatorArchiveData["race"] = "未知";
                operatorArchiveData["drawer"] = "未知";
                operatorArchiveData["team_id"] = operatorJson["teamId"];
                operatorArchiveData["team"] = teamTable[operatorJson["teamId"]?.ToString() ?? ""]?["powerName"]?.ToString() ?? "未知";
                operatorArchiveData["group_id"] = operatorJson["groupId"];
                operatorArchiveData["group"] = teamTable[operatorJson["groupId"]?.ToString() ?? ""]?["powerName"]?.ToString() ?? "未知";
                operatorArchiveData["nation_id"] = operatorJson["nationId"];
                operatorArchiveData["nation"] = teamTable[operatorJson["nationId"]?.ToString() ?? ""]?["powerName"]?.ToString() ?? "未知";
                operatorArchiveData["birthday"] = "未知";

                operatorArchiveData["profile"] = operatorJson["itemUsage"]??"无";
                operatorArchiveData["impression"] = operatorJson["itemDesc"]??"无";
                
                operatorArchiveData["potential_item"] = itemTable[operatorJson["potentialItemId"]?.ToString() ?? ""]?["description"]?.ToString() ?? "";

                operatorArchiveData["limit"] = ""; //当前版本无法获取本数据
                operatorArchiveData["unavailable"] = ""; //当前版本无法获取本数据

                operatorArchiveData["is_recruit"] = ""; //当前版本无法获取本数据
                operatorArchiveData["is_classic"] = operatorJson["classicPotentialItemId"] != null;
                operatorArchiveData["is_sp"] = operatorJson["isSpChar"];
                
                //stories
                //stories_data = JsonData.get_json_data('handbook_info_table')['handbookDict']
                // stories = []
                // if self.id in stories_data:
                //     for item in stories_data[self.id]['storyTextAudio']:
                //         stories.append({'story_title': item['storyTitle'], 'story_text': item['stories'][0]['storyText']})   

                var stories = new JArray();
                if (handbookInfoTable?[operatorId] != null)
                {
                    foreach (var item in handbookInfoTable[operatorId]?["storyTextAudio"]!)
                    {
                        stories.Add(new JObject
                        {
                            {"story_title", item["storyTitle"]},
                            {"story_text", item["stories"]?[0]?["storyText"]}
                        });
                    }
                }
                operatorArchiveData["stories"] = stories;
                
                //CV
                var opCv = voiceLangDict?[operatorId];
                if (opCv != null)
                {
                    if (opCv["dict"] is JObject voiceLang)
                    {
                        var cvObject = new JObject();

                        foreach (var item in voiceLang)
                        {
                            var cvKey = voiceLangTypeDict[item.Key]?["name"]?.ToString();
                            if (cvKey != null)
                            {
                                cvObject[cvKey] = item.Value?["cvName"];
                            }
                        }

                        operatorArchiveData["cv"] = cvObject;
                    }
                }
                
                //race
                foreach (var story in stories)
                {
                    if (story["story_title"]?.ToString() == "基础档案")
                    {
                        var raceReg = System.Text.RegularExpressions.Regex.Match(story["story_text"]?.ToString() ?? "",
                            @"\n【种族】.*?(\S+).*?\n");
                        if (raceReg.Success)
                        {
                            operatorArchiveData["race"] = raceReg.Groups[1].Value;
                        }

                        var sexReg = System.Text.RegularExpressions.Regex.Match(story["story_text"]?.ToString() ?? "",
                                                       @"\n【性别】.*?(\S+).*?\n");
                        if (sexReg.Success)
                        {
                            operatorArchiveData["sex"] = sexReg.Groups[1].Value;
                        }
                    }
                }

                //tags

                //drawer

                //range

                //origin

                //extra

                //skin
                //self.__skins_list = sorted(Collection.get_skins_list(code), key = lambda n: n['displaySkin']['getTime'])
                var skins = skinTable?.Where(s => (s as JProperty).Value["charId"]?.ToString() == operatorId)
                    .Select(s=> (s as JProperty).Value).ToList();
                if (skins != null)
                {
                    var skinList = new JArray();
                    int skinSort = 0;
                    var skinLevels = new Dictionary<string, (string, string)>
                    {
                        { "1", ("初始", "stage0") },
                        { "1+", ("精英一", "stage1") },
                        { "2", ("精英二", "stage2") }
                    };

                    foreach (var skin in skins)
                    {
                        var skinDisplayData = skin["displaySkin"];
                        if (skinDisplayData == null)
                        {
                            continue;
                        }

                        var skinId = skin["skinId"]?.ToString();
                        if (skinId == null)
                        {
                            continue;
                        }

                        var skinInfo = skinId.Split('#');
                        var skinIndex = skinInfo.Length>1?skinInfo[1]:null;
                        string skinName = string.Empty;
                        string skinKey;

                        if (skinIndex!=null && !skinId.Contains("@"))
                        {
                            var skinLevelInfo = skinLevels[skinIndex];
                            skinName = skinLevelInfo.Item1;
                            skinKey = skinLevelInfo.Item2;
                        }
                        else
                        {
                            skinSort += 1;
                            skinKey = $"skin{skinSort}";
                        }

                        var skinData = new JObject
                        {
                            ["skin_id"] = skinId,
                            ["skin_key"] = skinKey,
                            ["skin_name"] = skinDisplayData["skinName"]?.ToString() ?? skinName,
                            ["skin_drawer"] = skinDisplayData["drawerList"]?.LastOrDefault()?.ToString() ?? string.Empty,
                            ["skin_group"] = skinDisplayData["skinGroupName"]?.ToString() ?? string.Empty,
                            ["skin_content"] = skinDisplayData["dialog"]?.ToString() ?? string.Empty,
                            ["skin_usage"] = skinDisplayData["usage"]?.ToString() ?? $"{skinName}立绘",
                            ["skin_desc"] = skinDisplayData["description"]?.ToString() ?? string.Empty,
                            ["skin_source"] = skinDisplayData["obtainApproach"]?.ToString() ?? string.Empty
                        };

                        skinList.Add(skinData);
                    }

                    operatorArchiveData["skins"] = skinList;
                    //所有画师逗号分隔
                    operatorArchiveData["drawer"] = string.Join(",", skinList.Select(s => s["skin_drawer"]?.ToString()).Distinct());
                }



                //skill
                // TODO: SkillList
                var skills = new JArray();
                var skillsId = new JArray();
                var skillsCost = new JArray();
                var skillsDesc = new JObject();

                var skillLevelUpData = operatorJson["allSkillLvlup"]?.ToObject<JArray>();

                if (skillLevelUpData != null)
                {
                    int index = 1;
                    foreach (var item in skillLevelUpData)
                    {
                        var lvlUpCost = item["lvlUpCost"]?.ToObject<JArray>();
                        if (lvlUpCost != null)
                        {
                            foreach (var cost in lvlUpCost)
                            {
                                var skillCost = new JObject
                                {
                                    ["skill_no"] = null,
                                    ["level"] = index + 1,
                                    ["mastery_level"] = 0,
                                    ["use_material_id"] = cost["id"],
                                    ["use_number"] = cost["count"]
                                };
                                skillsCost.Add(skillCost);
                            }
                        }
                        index++;
                    }
                }

                foreach (var skill in operatorJson["skills"]?.ToObject<JArray>())
                {
                    var code = skill["skillId"]?.ToString();

                    if (code == null || !skillTable.ContainsKey(code))
                    {
                        continue;
                    }

                    var detail = skillTable[code];
                    var icon = detail["iconId"]?.ToString() ?? detail["skillId"]?.ToString();

                    if (detail == null)
                    {
                        continue;
                    }

                    skillsId.Add(code);

                    if (!skillsDesc.ContainsKey(code))
                    {
                        skillsDesc[code] = new JArray();
                    }

                    int levelIndex = 1;
                    foreach (var desc in detail["levels"]?.ToObject<JArray>())
                    {
                        var description = ParseTemplate(desc["blackboard"], desc["description"]?.ToString());

                        var skillRange = operatorJson["range"]?.ToString();
                        if (desc["rangeId"] != null && rangeTable.ContainsKey(desc["rangeId"].ToString()))
                        {
                            skillRange = BuildRange(rangeTable[desc["rangeId"].ToString()]["grids"]?.ToObject<JArray>());
                        }

                        var skillDesc = new JObject
                        {
                            ["skill_level"] = levelIndex,
                            ["skill_type"] = desc["skillType"],
                            ["sp_type"] = desc["spData"]?["spType"],
                            ["sp_init"] = desc["spData"]?["initSp"],
                            ["sp_cost"] = desc["spData"]?["spCost"],
                            ["duration"] = Convert.ToInt32(desc["duration"]),
                            ["description"] = description.Replace("\\n", "\n"),
                            ["max_charge"] = desc["spData"]?["maxChargeTime"],
                            ["range"] = skillRange
                        };

                        ((JArray)skillsDesc[code]).Add(skillDesc);
                        levelIndex++;
                    }

                    var levelUpCostData = skill["specializeLevelUpData"]?.ToObject<JArray>() ?? skill["levelUpCostCond"]?.ToObject<JArray>();

                    if (levelUpCostData != null)
                    {
                        int levIndex = 1;
                        foreach (var cond in levelUpCostData)
                        {
                            var levelUpCost = cond["levelUpCost"]?.ToObject<JArray>();
                            if (levelUpCost != null)
                            {
                                foreach (var cost in levelUpCost)
                                {
                                    var skillCost = new JObject
                                    {
                                        ["skill_no"] = code,
                                        ["level"] = levIndex + 7,
                                        ["mastery_level"] = levIndex,
                                        ["use_material_id"] = cost["id"],
                                        ["use_number"] = cost["count"]
                                    };
                                    skillsCost.Add(skillCost);
                                }
                            }
                            levIndex++;
                        }
                    }

                    var skillEntry = new JObject
                    {
                        ["skill_no"] = code,
                        ["skill_index"] = skills.Count + 1,
                        ["skill_name"] = detail["levels"]?[0]?["name"]?.ToString(),
                        ["skill_icon"] = icon
                    };

                    skills.Add(skillEntry);
                }

                operatorArchiveData["skills"] = skills;
                operatorArchiveData["skills_id"] = skillsId;
                operatorArchiveData["skills_cost"] = skillsCost;
                operatorArchiveData["skills_desc"] = skillsDesc;


                operatorArchive[operatorId] = operatorArchiveData;
            }

            LoadJson(operatorArchive, "operator_archive.json");
        }
    }
}
