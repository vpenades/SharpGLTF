using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Materials
{
    public class MaterialBuilder
    {
        #region data

        private readonly List<Channel> _Channels = new List<Channel>();

        #endregion

        #region properties

        public string Name { get; set; }

        public IReadOnlyCollection<Channel> Channels => _Channels;

        public Schema2.AlphaMode Alpha { get; set; } = Schema2.AlphaMode.OPAQUE;

        public Single AlphaCutoff { get; set; } = 0.5f;

        public Boolean DoubleSided { get; set; } = false;

        public Boolean Unlit { get; set; } = false;

        public String Style { get; set; } = "PBRMetallicRoughness";

        #endregion

        #region API

        public Channel GetChannel(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            key = key.ToLower();

            return _Channels.FirstOrDefault(item => string.Equals(key, item.Key, StringComparison.OrdinalIgnoreCase));
        }

        public Channel UseChannel(string key)
        {
            var ch = GetChannel(key);

            if (ch != null) return ch;

            ch = new Channel(key);
            _Channels.Add(ch);

            return ch;
        }

        #endregion
    }
}
