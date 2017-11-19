using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XDevs
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class ServiceSettings : ScriptableObject
    {
        public List<Service> services;

        private const string serviceSettingsAssetName = "ServiceSettings";
        private const string serviceSettingsAssetExtension = ".asset";

        private static ServiceSettings instance;

        private static ServiceSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance =
                        Resources.Load(Path.Combine(GameManager.CurrentResourcesFolder, serviceSettingsAssetName)) as
                            ServiceSettings;

                    if (instance == null)
                    {
                        instance = CreateInstance<ServiceSettings>();

#if UNITY_EDITOR
                        var serviceSettingsPath = Path.Combine("Resources", GameManager.CurrentResourcesFolder);

                        string properPath = Path.Combine(Application.dataPath, serviceSettingsPath);

                        if (!Directory.Exists(properPath))
                        {
                            Directory.CreateDirectory(properPath);
                        }

                        string fullPath = Path.Combine(
                            Path.Combine("Assets", serviceSettingsPath),
                            serviceSettingsAssetName + serviceSettingsAssetExtension);

                        AssetDatabase.CreateAsset(instance, fullPath);
#endif
                    }
                }

                return instance;
            }
        }

        public static ServiceSettings Services { get { return Instance; } }

        public Service this[ServiceSettingsKeys.Service service]
        {
            get
            {
                if (services == null)
                {
                    services = new List<Service>
                    {
                        new Service
                        {
                            name = service,
                        },
                    };
                }

                Service requestedService = services.FirstOrDefault(srvs => srvs.name == service);

                if (requestedService == null)
                {
                    requestedService = new Service
                    {
                        name = service,
                    };

                    services.Add(requestedService);
                }

                Save();

                return requestedService;
            }
        }

        public static void Save()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(Instance);
#endif
        }
    }

    [Serializable]
    public class Service
    {
        public ServiceSettingsKeys.Service name;
        public List<Field> fields;

        public string this[ServiceSettingsKeys.Field field]
        {
            get
            {
                Field requestedField = null;

                if (fields != null)
                    requestedField = fields.FirstOrDefault(fld => fld.name == field);

                if (requestedField != null)
                    return requestedField.value;

                Debug.LogErrorFormat("Field {0} doesn't exist in ServiceSettings!", field);

                return string.Empty;
            }
            set
            {
                if (fields == null)
                {
                    fields = new List<Field>
                        {
                            new Field
                            {
                                name = field,
                                value = value
                            },
                        };
                }
                else
                {
                    if (fields.Any(fld => fld.name == field))
                        fields.First(fld => fld.name == field).value = value;
                    else
                        fields.Add(
                            new Field
                            {
                                name = field,
                                value = value
                            });

                    ServiceSettings.Save();
                }

            }
        }
    }

    [Serializable]
    public class Field
    {
        [SerializeField]
        internal ServiceSettingsKeys.Field name;

        [SerializeField]
        internal string value;
    }
}