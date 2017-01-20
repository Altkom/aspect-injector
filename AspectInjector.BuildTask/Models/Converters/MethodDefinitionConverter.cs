using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspectInjector.BuildTask.Models.Converters
{
    internal class MethodDefinitionConverter : JsonConverter
    {
        private readonly ModuleDefinition _reference;

        public MethodDefinitionConverter(ModuleDefinition reference)
        {
            _reference = reference;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MethodDefinition);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jt = JToken.Load(reader);
            var tokenRefs = jt.ToObject<string>().Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var tdToken = new MetadataToken(uint.Parse(tokenRefs[0]));
            var mdToken = new MetadataToken(uint.Parse(tokenRefs[1]));

            return _reference.GetTypes().First(td => td.MetadataToken == tdToken).Methods.First(md => md.MetadataToken == mdToken);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var md = (MethodDefinition)value;
            var td = md.DeclaringType;

            var tokenRef = $"{td.MetadataToken.ToUInt32()}:{md.MetadataToken.ToUInt32()}";

            JToken.FromObject(tokenRef, serializer).WriteTo(writer);
        }
    }
}