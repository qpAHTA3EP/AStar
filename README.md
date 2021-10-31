# Описание
Данная библиотека используется ботом [Astral](https://www.neverwinter-bot.com/forums/index.php) к MMORPG ["Neverwinter Online"](https://www.arcgames.com/en/games/neverwinter/news).
Она предназначена для поиска пути в ориентированном графе, вершинами которого являются точки в трехмерном игровом пространстве.  
Заложенные в алгоритме AStar эвристики часто приводят к нахождению неоптимального пути, проходящего через локальные петли направленные в строну конечной точки, однако, приводящие к увеличению длины пути.  

<p align="center"><img src="AStar/img/AStar.png"></p>

Поскольку путевые графы в игре ["Neverwinter Online"](https://www.arcgames.com/en/games/neverwinter/news) являются слабосвязанными хорошие результаты показывает волновой поиск.  
В целях оптимизации результаты поиска кэшируются и могут использоваться повторно.

<p align="center"><img src="AStar/img/WaveSearch.png"></p>