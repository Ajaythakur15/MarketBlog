using MarketBlog;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseStaticFiles();
var dataFile = Path.Combine(app.Environment.ContentRootPath, "App_Data", "posts.json");
Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
var gate = new SemaphoreSlim(1, 1);

async Task<List<Post>> LoadPosts()
{
    await gate.WaitAsync();
    try { return File.Exists(dataFile) ? JsonSerializer.Deserialize<List<Post>>(await File.ReadAllTextAsync(dataFile)) ?? [] : []; }
    finally { gate.Release(); }
}
async Task SavePosts(List<Post> posts)
{
    await gate.WaitAsync();
    try { await File.WriteAllTextAsync(dataFile, JsonSerializer.Serialize(posts, jsonOptions)); }
    finally { gate.Release(); }
}
string E(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");
string Slug(string text) { var s = Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-'); return string.IsNullOrEmpty(s) ? "post" : s; }
string Layout(string title, string body) => $"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{E(title)} · Aj Type & Idea</title><link rel="stylesheet" href="/style.css"></head><body><header class="top"><a class="brand" href="/">Aj Type<span>&</span>IDEA</a><nav><a href="/">Explore</a><a href="/write" class="nav-write">Write a story <b>↗</b></a></nav></header>{body}<footer><span>Aj Type & IDEA</span><span>Good ideas deserve to be read.</span></footer></body></html>""";
string ReadTime(string body) => $"{Math.Max(1, (int)Math.Ceiling(body.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 200.0))} min read";
string Card(Post p) => $"""<article class="card"><div class="card-meta"><span class="tag">{E(string.IsNullOrWhiteSpace(p.Category) ? "Notes" : p.Category)}</span><span>{p.PublishedAt:MMM d, yyyy}</span></div><h2><a href="/p/{E(p.Slug)}">{E(p.Title)}</a></h2><p>{E(p.Excerpt)}</p><div class="byline"><span class="avatar">{E((p.Author.Length > 0 ? p.Author[0] : 'A').ToString().ToUpper())}</span><span>{E(p.Author)} <small>· {ReadTime(p.Content)}</small></span><a class="arrow" href="/p/{E(p.Slug)}">↗</a></div></article>""";

app.MapGet("/", async () =>
{
    var all = await LoadPosts(); var published = all.Where(p => p.IsPublished).OrderByDescending(p => p.PublishedAt).ToList();
    var cards = published.Count == 0 ? "<div class='empty'><div class='empty-icon'>✳</div><h2>Your first story starts here.</h2><p>Write something worth sharing, then publish it for the world to read.</p><a class='button' href='/write'>Create your first post <span>↗</span></a></div>" : $"<div class='grid'>{string.Join("", published.Select(Card))}</div>";
    var drafts = all.Where(p => !p.IsPublished).OrderByDescending(p => p.UpdatedAt).ToList();
    var draftSection = drafts.Count == 0 ? "" : $"<section class='drafts'><h3>Your drafts <span>{drafts.Count}</span></h3><div class='draft-list'>{string.Join("", drafts.Select(p => $"<a href='/edit/{p.Id}'><span>{E(p.Title)}</span><small>Edited {p.UpdatedAt:MMM d}</small><b>→</b></a>"))}</div></section>";
    return Results.Content(Layout("Stories for curious minds", $"<main><section class='hero'><div class='eyebrow'><i></i> A HOME FOR YOUR IDEAS</div><h1>Make room for<br><em>good ideas.</em></h1><p>A little corner of the internet for your stories, thoughts, and things worth sharing.</p><a class='button' href='/write'>Start writing <span>↗</span></a><div class='hero-note'>YOUR WORDS. YOUR SPACE. OUT IN THE WORLD.</div></section><section class='feed-head'><div><div class='eyebrow'>THE LATEST</div><h2>Fresh off the page</h2></div><span>{published.Count} {(published.Count == 1 ? "story" : "stories")}</span></section>{cards}{draftSection}</main>"), "text/html");
});

app.MapGet("/write", () => Results.Content(Layout("Write a story", Form(null)), "text/html"));
app.MapGet("/edit/{id:guid}", async (Guid id) => { var p=(await LoadPosts()).FirstOrDefault(x=>x.Id==id); return p is null ? Results.NotFound() : Results.Content(Layout("Edit story", Form(p)), "text/html"); });
app.MapPost("/save", async (HttpRequest req) =>
{
    var f = await req.ReadFormAsync();
    var title = f["title"].ToString().Trim();
    var content = f["content"].ToString().Trim();
    var author = f["author"].ToString().Trim();
    var category = f["category"].ToString().Trim();
    var excerpt = f["excerpt"].ToString().Trim();
    var values = f.ToDictionary(x => x.Key, x => x.Value);
    var errors = PostInputValidator.Validate(title, content, author, category, excerpt);
    if (errors.Count > 0)
        return Results.Content(Layout("Check your story", FormData(values, errors)), "text/html", statusCode: 400);
    var posts = await LoadPosts(); var hasId = Guid.TryParse(f["id"], out var id); var post = hasId ? posts.FirstOrDefault(x=>x.Id==id) : null;
    if (post is null) { post = new Post { Id=Guid.NewGuid(), CreatedAt=DateTimeOffset.UtcNow }; posts.Add(post); }
    post.Title=title; post.Content=content; post.Author=author; post.Category=category;
    post.Excerpt=string.IsNullOrWhiteSpace(excerpt) ? (content.Length <= 180 ? content : content[..177] + "...") : excerpt;
    post.Slug=Slug(title); post.UpdatedAt=DateTimeOffset.UtcNow;
    var requestedPublish=f["publish"]=="true"; if(requestedPublish && !post.IsPublished) post.PublishedAt=DateTimeOffset.UtcNow; post.IsPublished=requestedPublish;
    var baseSlug=post.Slug; var n=2; while(posts.Any(x=>x.Id!=post.Id && x.Slug==post.Slug)) post.Slug=$"{baseSlug}-{n++}";
    await SavePosts(posts); return Results.Redirect(requestedPublish ? $"/p/{post.Slug}?new=1" : "/?saved=1");
});
app.MapPost("/delete/{id:guid}", async (Guid id) => { var posts=await LoadPosts(); posts.RemoveAll(p=>p.Id==id); await SavePosts(posts); return Results.Redirect("/"); });
app.MapGet("/p/{slug}", async (string slug, HttpRequest req) =>
{
    var p=(await LoadPosts()).FirstOrDefault(x=>x.Slug==slug && x.IsPublished); if(p is null) return Results.NotFound();
    var content=string.Join("<br><br>", E(p.Content).Split("\n\n").Select(x=>x.Replace("\n","<br>")));
    var share=req.Query.ContainsKey("new") ? "<div class='share-banner'>Your story is live. Copy the link and share it with the world. <button onclick='navigator.clipboard.writeText(location.origin+location.pathname).then(()=>this.innerText=\"Copied!\")'>Copy link ↗</button></div>" : "";
    var body=$"""<main class="article-wrap">{share}<a class="back" href="/">← All stories</a><div class="article-meta"><span class="tag">{E(string.IsNullOrWhiteSpace(p.Category)?"Notes":p.Category)}</span><span>{p.PublishedAt:MMMM d, yyyy} · {ReadTime(p.Content)}</span></div><h1 class="article-title">{E(p.Title)}</h1><p class="article-excerpt">{E(p.Excerpt)}</p><div class="article-author"><span class="avatar">{E((p.Author.Length > 0 ? p.Author[0] : 'A').ToString().ToUpper())}</span><div><b>{E(p.Author)}</b><small>Author</small></div></div><hr><div class="prose">{content}</div><div class="article-end">✳</div><div class="share-bottom"><div><b>Enjoyed this story?</b><span>Pass it along to someone who'd love it.</span></div><button onclick='navigator.clipboard.writeText(location.href).then(()=>this.innerText="Link copied!")'>Share story ↗</button></div></main>""";
    return Results.Content(Layout(p.Title,body),"text/html");
});

string Form(Post? p) => FormData(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["id"]=p?.Id.ToString()??"", ["title"]=p?.Title??"", ["content"]=p?.Content??"", ["author"]=p?.Author??"", ["category"]=p?.Category??"", ["excerpt"]=p?.Excerpt??"" }, null, p?.IsPublished??false);
string FormData(Dictionary<string,Microsoft.Extensions.Primitives.StringValues> f, Dictionary<string,string>? errors, bool isPublished=false)
{
    string V(string k)=>E(f.TryGetValue(k,out var v)?v.ToString():"");
    string Error(string key) => errors is not null && errors.TryGetValue(key, out var message) ? $"<span class=\"field-error\" id=\"{key}-error\">{E(message)}</span>" : "";
    string Invalid(string key) => errors?.ContainsKey(key) == true ? "true" : "false";
    var exists=!string.IsNullOrWhiteSpace(V("id"));
    var summary = errors is { Count: > 0 } ? $"<div class=\"validation-summary\" role=\"alert\"><b>Almost there — please check:</b><ul>{string.Join("", errors.Select(item => $"<li><a href=\"#{item.Key}\">{E(item.Value)}</a></li>"))}</ul></div>" : "";
    return $"""<main class="editor"><a class="back" href="/">← Back to stories</a><div class="eyebrow editor-label"><i></i> THE WRITING ROOM</div><h1>{(exists?"Give it another pass.":"What’s on your mind?")}</h1><p class="editor-intro">A blank page is full of possibility. Start wherever you like.</p>{summary}<form method="post" action="/save" novalidate><input type="hidden" name="id" value="{V("id")}"><label for="title">TITLE <span class="required-mark">*</span></label><input class="title-input {(Invalid("title") == "true" ? "invalid" : "")}" id="title" name="title" maxlength="120" aria-invalid="{Invalid("title")}" aria-describedby="title-error" placeholder="Give your story a name…" value="{V("title")}" required>{Error("title")}<div class="form-row"><div><label for="author">YOUR NAME <span class="required-mark">*</span></label><input id="author" name="author" maxlength="60" aria-invalid="{Invalid("author")}" aria-describedby="author-error" placeholder="How readers know you" value="{V("author")}" required>{Error("author")}</div><div><label for="category">TOPIC <span class="optional">OPTIONAL · 40 CHARACTERS</span></label><input id="category" name="category" maxlength="40" aria-invalid="{Invalid("category")}" aria-describedby="category-error" placeholder="Ideas, life, craft…" value="{V("category")}">{Error("category")}</div></div><label for="excerpt">A SHORT INTRO <span class="optional">OPTIONAL · 220 CHARACTERS</span></label><input id="excerpt" name="excerpt" maxlength="220" aria-invalid="{Invalid("excerpt")}" aria-describedby="excerpt-error" placeholder="A one-line preview for the story card" value="{V("excerpt")}">{Error("excerpt")}<label for="content">YOUR STORY <span class="required-mark">*</span></label><textarea id="content" name="content" rows="15" maxlength="50000" aria-invalid="{Invalid("content")}" aria-describedby="content-error" placeholder="The first sentence is the only hard part…" required>{V("content")}</textarea><div class="field-hint"><span>{Error("content")}</span><span id="story-count">{V("content").Length} / 50,000</span></div><div class="form-actions"><a href="/">Cancel</a><button class="button button-draft" type="submit" name="publish" value="false">Save draft</button><button class="button" type="submit" name="publish" value="true">{(isPublished?"Update & publish":"Publish story")} <span>↗</span></button></div>{(exists?"<div class='delete-row'><button form='delete-form' class='delete' type='submit'>Delete this story</button></div>":"")}</form>{(exists?$"<form id='delete-form' method='post' action='/delete/{V("id")}' onsubmit='return confirm(&quot;Delete this story permanently?&quot;)'></form>":"")}<p class="privacy-note">Your drafts stay here until you publish them.</p><script>const story=document.getElementById('content'),count=document.getElementById('story-count');story?.addEventListener('input',()=>count.textContent=story.value.length.toLocaleString()+' / 50,000');</script></main>""";
}
app.Run();

class Post { public Guid Id {get;set;} public string Title {get;set;}=""; public string Content {get;set;}=""; public string Author {get;set;}=""; public string Category {get;set;}=""; public string Excerpt {get;set;}=""; public string Slug {get;set;}=""; public bool IsPublished {get;set;} public DateTimeOffset CreatedAt {get;set;}=DateTimeOffset.UtcNow; public DateTimeOffset UpdatedAt {get;set;}=DateTimeOffset.UtcNow; public DateTimeOffset PublishedAt {get;set;}=DateTimeOffset.UtcNow; }






