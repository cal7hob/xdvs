using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ServiceSettings : MonoBehaviour
{
    [Serializable]
    public class Service
    {
        public ServiceSettingsKeys.Service name;
        public Field[] fields;

        public string this[ServiceSettingsKeys.Field field]
        {
            get
            {
                Field requestedField = fields.FirstOrDefault(fld => fld.name == field);

                if (requestedField != null)
                    return requestedField.value;

                Debug.LogErrorFormat(
                    "Field {0} doesn't exist in ServiceSettings!"
                        + "You may have to set it in ServiceSettings prefab!",
                    field);

                return String.Empty;
            }
            set
            {
                fields.First(fld => fld.name == field).value = value;
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

    public Service[] services;

    void Awake()
    {
        if (Services != null)
        {
            Destroy(gameObject);
            return;
        }

        Services = this;

        DontDestroyOnLoad(gameObject);
    }

    public static ServiceSettings Services { get; private set; }

    public Service this[ServiceSettingsKeys.Service service]
    {
        get
        {
            Service requestedService = services.FirstOrDefault(srvs => srvs.name == service);

            if (requestedService != null)
                return requestedService;

            Debug.LogErrorFormat(
                "Service {0} doesn't exist in ServiceSettings! "
                    + "You may have to set it in ServiceSettings prefab!",
                service);

            return null;
        }
    }

    #if UNITY_EDITOR
    public void Save() { EditorUtility.SetDirty(this); }
    #endif
}
