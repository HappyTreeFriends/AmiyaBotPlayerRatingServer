using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
                if (obtain != "凭证交易所" && obtain != "招募寻访" &&
                    obtain != "活动获得" && obtain != "主线剧情" &&
                    obtain != "信用交易所")
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
                        var r = System.Text.RegularExpressions.Regex.Match(story["story_text"]?.ToString() ?? "",
                            @"\n【种族】.*?(\S+).*?\n");
                        if (r.Success)
                        {
                            operatorArchiveData["race"] = r.Groups[1].Value;
                            break;
                        }
                    }
                }
                
                //tags

                //drawer

                //range

                //origin

                //extra

                //skin
                // TODO: SkinList

                //skill
                // TODO: SkillList

                operatorArchive[operatorId] = operatorArchiveData;
            }

            LoadJson(operatorArchive, "operator_archive.json");
        }
    }
}
