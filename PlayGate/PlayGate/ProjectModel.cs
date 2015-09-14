using Newtonsoft.Json;
using System.IO;

namespace PlayGate
{
    public class ProjectModel
    {
        #region Public static data.

        public static readonly string PROJECT_FILE_EXTENSION = ".pgproject";

        #endregion



        #region Public properties.

        public string Name { get; set; }
        [JsonIgnore]
        public string ProjectFilePath { get; set; }
        [JsonIgnore]
        public string WorkingDirectory { get; set; }

        #endregion



        #region Construction and destruction.

        public ProjectModel()
        {
            Name = "";
            WorkingDirectory = "";
            ProjectFilePath = "";
        }

        public void Setup(string name, string path)
        {
            Name = name;
            WorkingDirectory = path;
            ProjectFilePath = Path.Combine(path, name + PROJECT_FILE_EXTENSION);
        }

        #endregion
    }
}
