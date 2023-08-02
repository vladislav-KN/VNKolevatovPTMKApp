using Faker;
namespace PTMKApp;

public class Entity
{
    public string? FIO { get; private set; }
    public DateTime? BirthDay { get; private set;}
    public Gender? UserGender { get; private set; }

    public Entity()
    {
        FIO = null;
        BirthDay = null;
        UserGender = null;
    }
    public Entity(string fio, DateTime birthDay, Gender gender)
    {
        FIO = fio;
        BirthDay = birthDay;
        UserGender = gender;

    }
     
    DateTime RandomDay(Random gen)
    {
        DateTime start = new DateTime(1950, 1, 1);
        int range = (DateTime.Today - start).Days;           
        return start.AddDays(gen.Next(range));
    }
    public bool IsMale()
    {
        if (UserGender == Gender.Male)
            return true;
        else
            return false;
    }

    public void Randomize()
    {
        Random gen = new Random();
        FIO = Faker.Name.FullName();
        BirthDay = RandomDay(gen);
        Random random = new Random();
        if(random.Next(2)==1)
            UserGender = Gender.Male;
        else
            UserGender = Gender.Female;

    }
    public void Randomize(string nameBegin)
    {
        Random gen = new Random();
        FIO = Faker.Name.FullName();
        while(!FIO.StartsWith(nameBegin)) 
        {
            FIO = Faker.Name.FullName();
        }
 
        BirthDay = RandomDay(gen);
        Random random = new Random();
        if(random.Next(2)==1)
            UserGender = Gender.Male;
        else
            UserGender = Gender.Female;
    }
}
public enum Gender
{
    Male = 0,
    Female = 1
}