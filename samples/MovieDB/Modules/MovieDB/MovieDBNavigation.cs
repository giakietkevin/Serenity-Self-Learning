using Serenity.Navigation;
using MyPages = Serene.MovieDB.Pages;

// Bài 07: menu "Movie Database" nằm ngay dưới Dashboard (1000) và trước Northwind (7000).
[assembly: NavigationMenu(2000, "Movie Database", icon: "fa-film")]
[assembly: NavigationLink(2100, "Movie Database/Movies", typeof(MyPages.MovieController), icon: "fa-video-camera")]
[assembly: NavigationLink(2200, "Movie Database/Genres", typeof(MyPages.GenreController), icon: "fa-tags")]
