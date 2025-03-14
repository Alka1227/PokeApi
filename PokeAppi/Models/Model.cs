using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace PokeAppi.Models
{
   public class Pokemon
        {
            [JsonPropertyName("id")]
            public int ID { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("weight")]
            public int Weight { get; set; }

            [JsonPropertyName("sprites")]
            public Sprites Sprites { get; set; }

            [JsonPropertyName("types")]
            public List<PokemonTypeWrapper> Types { get; set; }
        }
        public class Sprites
        {
            [JsonPropertyName("front_default")]
            public string Front_Default { get; set; }
        }

        public class PokemonTypeWrapper
        {
            [JsonPropertyName("type")]
            public PokemonType Type { get; set; }
        }

        public class PokemonType
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
        public class PokemonTypeResponse
        {
            [JsonPropertyName("pokemon")]
            public List<PokemonEntry> Pokemon { get; set; }
        }
        public class PokemonEntry
        {
            [JsonPropertyName("pokemon")]
            public PokemonBasicInfo Pokemon { get; set; }
        }

        public class PokemonBasicInfo
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
}
    
