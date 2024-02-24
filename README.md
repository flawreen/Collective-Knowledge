# MVC Stackoverflow web app

## Technologies
<img src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" /> <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" />
<img src="https://img.shields.io/badge/Microsoft%20SQL%20Server-CC2927?style=for-the-badge&logo=microsoft%20sql%20server&logoColor=white" />
<img src="https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white" />
<img src="https://img.shields.io/badge/GIT-E44C30?style=for-the-badge&logo=git&logoColor=white" />


[Demo Video](https://github.com/flawreen/MVC-StackOverflow/assets/83332450/7a9fa953-0fdc-4287-a74d-bf38cc5717ee)


[Alternative Demo Link](https://streamable.com/2hwpzl)


### Max-heap search engine [link](/Controllers/DiscussionsController.cs)

```
public IQueryable<Discussion> Search(string searchValue)
{
    if (searchValue == null || searchValue.Length == 0)
    {
        return null;
    }
    // regex care ia doar litere si cifre
    var regex = new Regex(@"\W+");
    // lista de keywords
    var keywords = regex.Split(searchValue.ToLower());
    // iau obiectele din baza de date
    var discussions = db.Discussions.Include("Answers").Include("Category");
    // max-heap: cele mai relevante discutii gasite
    var rez = new PriorityQueue<Discussion, int>(Comparer<int>.Create((a, b) => b - a));

    foreach (var discussion in discussions)
    {
        // fac split sa iau doar cuvintele sub forma de lista din titlu, description + raspunsuri
        var allWords = regex.Split(discussion.Title.ToLower()).Union(regex.Split(discussion.Content.ToLower())).ToList();
        foreach (var comm in discussion.Answers)
        {
            allWords.Union(regex.Split(comm.Content.ToLower()));
        }

        // relevanta = cate keyword-uri se regasesc in titlu, descriere si raspunsuri
        int relevance = allWords.Intersect(keywords).Count();
        if (relevance > 0)
        {
            rez.Enqueue(discussion, relevance);  // adaug in max-heap
        }
    }

    // scot in ordine obiectele din max-heap si le incarc intr-o lista
    List<Discussion> res = new List<Discussion>();
    while (rez.TryDequeue(out var obj, out var priority))
    {
        res.Add(obj);
    }

    // nu s-au gasit rezultate
    if (!res.Any())
    {
        return null;
    }

    return res.AsQueryable();
}
```




