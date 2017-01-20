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
    internal class TypeDefinitionConverter : JsonConverter
    {
        private readonly ModuleDefinition _reference;

        public TypeDefinitionConverter(ModuleDefinition reference)
        {
            _reference = reference;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TypeDefinition);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jt = JToken.Load(reader);
            var token = new MetadataToken(jt.ToObject<uint>());

            return _reference.GetTypes().First(td => td.MetadataToken == token);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var td = (TypeDefinition)value;

            var token = td.MetadataToken.ToUInt32();

            JToken.FromObject(token, serializer).WriteTo(writer);
        }
    }
}