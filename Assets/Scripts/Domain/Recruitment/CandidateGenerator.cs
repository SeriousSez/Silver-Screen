using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;
namespace SilverScreen.Domain.Recruitment
{
 public interface IRandomSource{int Next(int minimumInclusive,int maximumExclusive);}
 public sealed class SeededRandomSource:IRandomSource{private readonly Random _random;public SeededRandomSource(int seed)=>_random=new Random(seed);public int Next(int min,int max)=>_random.Next(min,max);}
 public class CandidateGenerator
 {
  private static readonly string[] First={"Ada","Beatrice","Clifford","Della","Edwin","Florence","Gideon","Hazel","Irene","Jasper","Lillian","Mabel","Nolan","Opal","Percy","Rosalie"};
  private static readonly string[] Last={"Archer","Bennett","Calloway","Dawson","Ellery","Finch","Griffin","Hollis","Keaton","Langley","Mercer","North","Pryce","Rowan","Sinclair","Vale"};
  private static readonly string[] Genres={"drama","comedy","action","romance","thriller","horror"};
  private static readonly ProfessionalRole[] RecruitableRoles={ProfessionalRole.Actor,ProfessionalRole.Director,ProfessionalRole.Extra,ProfessionalRole.Writer};
  private int _sequence;
  private readonly IRandomSource _random; public CandidateGenerator(IRandomSource random)=>_random=random??throw new ArgumentNullException(nameof(random));
  public virtual Candidate Generate(SimulationDateTime now)=>GenerateForRole(now,RecruitableRoles[_random.Next(0,RecruitableRoles.Length)]);
  public virtual Candidate GenerateForRole(SimulationDateTime now,ProfessionalRole role)=>GenerateForRole(now,role,false);
  public virtual Candidate GenerateEntryLevelForRole(SimulationDateTime now,ProfessionalRole role)=>GenerateForRole(now,role,true);
  public virtual Candidate GenerateTalentApplicant(SimulationDateTime now)
  {
   // Independent undeveloped skills; rare promise is not an employment eligibility type.
   int Skill()=>_random.Next(0,20)==0?_random.Next(25,41):_random.Next(0,19);
   var genres=new List<GenreExperience>();foreach(var genre in Genres)genres.Add(new GenreExperience(genre,_random.Next(0,13)));
   var person=new PersonProfile($"talent-{now.Year}-{now.Month:D2}-{now.Day:D2}-{++_sequence}-{_random.Next(100000,999999)}",
    First[_random.Next(0,First.Length)]+" "+Last[_random.Next(0,Last.Length)],
    new SimulationDateTime(now.Year-_random.Next(18,46),1,1,0,0),ProfessionalRole.Unassigned,
    new TalentProfile(Skill(),Skill(),genres,Skill()),new[]{PersonTraitIds.All[_random.Next(0,PersonTraitIds.All.Length)]});
   return new Candidate(person,600+_random.Next(0,7)*50);
  }
  private Candidate GenerateForRole(SimulationDateTime now,ProfessionalRole role,bool entryLevel)
  {
   int age=_random.Next(role==ProfessionalRole.Director?24:18,61);int background=_random.Next(0,entryLevel?2:4);int basis=entryLevel?Math.Clamp(16+background*10+_random.Next(-4,9),10,38):Math.Clamp(24+background*14+_random.Next(-8,10),10,92);int secondaryMax=entryLevel?31:46;int acting=role==ProfessionalRole.Director||role==ProfessionalRole.Writer?_random.Next(10,secondaryMax):basis;int directing=role==ProfessionalRole.Director?basis:_random.Next(8,entryLevel?31:42);int writing=role==ProfessionalRole.Writer?basis:_random.Next(8,entryLevel?31:42);if(role==ProfessionalRole.Extra)acting=Math.Min(acting,58);
   int month=_random.Next(1,13),year=now.Year-age,day=_random.Next(1,SimulationDateTime.GetDaysInMonth(year,month)+1);var experience=new List<GenreExperience>();foreach(var genre in Genres)experience.Add(new GenreExperience(genre,Math.Clamp(background*(entryLevel?6:12)+_random.Next(0,entryLevel?10:19),0,80)));
   int traitCount=_random.Next(1,4);var traits=new List<string>();while(traits.Count<traitCount){var trait=PersonTraitIds.All[_random.Next(0,PersonTraitIds.All.Length)];if(!traits.Contains(trait))traits.Add(trait);}
   string name=First[_random.Next(0,First.Length)]+" "+Last[_random.Next(0,Last.Length)];string id=$"talent-{now.Year}-{now.Month:D2}-{now.Day:D2}-{++_sequence}-{_random.Next(100000,999999)}";var person=new PersonProfile(id,name,new SimulationDateTime(year,month,day,0,0),role,new TalentProfile(acting,directing,experience,writing),traits);
   int primary=role==ProfessionalRole.Director?directing:role==ProfessionalRole.Writer?writing:acting;int salary=Math.Max(500,500+primary*35+background*250);if(role==ProfessionalRole.Extra)salary=Math.Min(salary,1800);return new Candidate(person,salary);
  }
 }
}
