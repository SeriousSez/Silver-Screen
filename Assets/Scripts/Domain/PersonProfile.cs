using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;
namespace SilverScreen.Domain
{
 public enum ProfessionalRole { Actor, Director, Extra, Crew, Writer }
 [Serializable] public sealed class GenreExperience
 {
  public string GenreId { get; }
  public int Experience { get; private set; }
  public GenreExperience(string genreId,int experience){GenreId=genreId??string.Empty;Experience=Math.Clamp(experience,0,100);}
 }
 [Serializable] public sealed class TalentProfile
 {
  private readonly List<GenreExperience> _genreExperience;
  public int ActingAbility { get; private set; }
  public int DirectingAbility { get; private set; }
  public int WritingAbility { get; private set; }
  public IReadOnlyList<GenreExperience> GenreExperience=>_genreExperience;
  public TalentProfile(int actingAbility,int directingAbility,IEnumerable<GenreExperience> genreExperience=null,int writingAbility=25){ActingAbility=Math.Clamp(actingAbility,0,100);DirectingAbility=Math.Clamp(directingAbility,0,100);WritingAbility=Math.Clamp(writingAbility,0,100);_genreExperience=genreExperience!=null?new List<GenreExperience>(genreExperience):new List<GenreExperience>();}
  public int GetGenreExperience(string genreId){foreach(var entry in _genreExperience)if(string.Equals(entry.GenreId,genreId,StringComparison.OrdinalIgnoreCase))return entry.Experience;return 0;}
  public void SetPrimaryAbility(ProfessionalRole role,int value){if(role==ProfessionalRole.Director)DirectingAbility=Math.Clamp(value,0,100);else if(role==ProfessionalRole.Writer)WritingAbility=Math.Clamp(value,0,100);else ActingAbility=Math.Clamp(value,0,100);}
 }
 public static class PersonTraitIds
 {
  public const string Ambitious="ambitious",Sociable="sociable",Reserved="reserved",Temperamental="temperamental",Patient="patient",Disciplined="disciplined",Confident="confident",Nervous="nervous";
  public static readonly string[] All={Ambitious,Sociable,Reserved,Temperamental,Patient,Disciplined,Confident,Nervous};
 }
 [Serializable] public sealed class PersonProfile
 {
  private readonly List<string> _traitIds;
  public string Id{get;} public string Name{get;private set;} public SimulationDateTime BirthDate{get;} public ProfessionalRole ProfessionalRole{get;private set;} public TalentProfile Talent{get;} public IReadOnlyList<string> TraitIds=>_traitIds; public string AppearanceProfileId{get;} public string VoiceProfileId{get;}
  public PersonProfile(string id,string name,SimulationDateTime birthDate,ProfessionalRole professionalRole,TalentProfile talent,IEnumerable<string> traitIds=null,string appearanceProfileId=null,string voiceProfileId=null){Id=string.IsNullOrWhiteSpace(id)?Guid.NewGuid().ToString():id;Name=string.IsNullOrWhiteSpace(name)?"Unnamed Talent":name.Trim();BirthDate=birthDate;ProfessionalRole=professionalRole;Talent=talent??new TalentProfile(0,0);_traitIds=traitIds!=null?new List<string>(traitIds):new List<string>();AppearanceProfileId=appearanceProfileId??string.Empty;VoiceProfileId=voiceProfileId??string.Empty;}
  public int GetAge(SimulationDateTime currentDate){int age=currentDate.Year-BirthDate.Year;if(currentDate.Month<BirthDate.Month||(currentDate.Month==BirthDate.Month&&currentDate.Day<BirthDate.Day))age--;return Math.Max(0,age);}
  public void Rename(string name){if(!string.IsNullOrWhiteSpace(name))Name=name.Trim();}
  public void SetProfessionalRole(ProfessionalRole role)=>ProfessionalRole=role;
 }
}
