using System;
using UnityEngine;

namespace OneMoreBuild
{
    public sealed partial class StudioApp
    {
        int platform, priceIndex = 1, audience, marketing, sequel = -1, selectedRelease = -1, reportTab;
        int SequelCandidate()
        {
            for (int i = S.releases.Count - 1; i >= 0; i--)
                if (!S.releases[i].legacy && S.releases[i].rating >= 6 && S.releases[i].genre == genre && S.releases[i].theme == theme) return i;
            return -1;
        }
        void DrawBusiness()
        {
            ModalHeader("План выпуска", "Сначала вложения, затем разработка и еженедельные продажи. Нет гарантии окупаемости.");
            Label(292, 211, 150, 24, "Платформа", small);
            for(int i=0;i<2;i++) if(Button(450+i*274,203,264,38,Market.Platforms[i],true,platform==i))platform=i;
            Label(292, 266, 150, 24, "Аудитория", small);
            for(int i=0;i<2;i++) if(Button(450+i*274,258,264,38,Market.Audiences[i],true,audience==i))audience=i;
            Label(292, 321, 150, 24, "Цена копии", small);
            for(int i=0;i<3;i++) if(Button(450+i*182,313,172,38,Market.Prices[i]+" мон.",true,priceIndex==i))priceIndex=i;
            Label(292, 376, 150, 24, "Реклама", small);
            for(int i=0;i<3;i++) if(Button(450+i*182,368,172,46,Market.Marketing[i]+"\n"+Market.MarketingCosts[i]+" мон.",true,marketing==i))marketing=i;
            int candidate = SequelCandidate();
            if(sequel != candidate) sequel=-1;
            if(Button(292,435,696,40,sequel>=0?"Продолжение: "+S.releases[sequel].title:"Новая серия · нажми для продолжения",candidate>=0,sequel>=0))sequel=sequel>=0?-1:candidate;
            int cost=(scope==0?90:150)+Market.MarketingCosts[marketing];
            Label(292,492,696,62,"Сейчас: "+cost+" мон. Содержание при разработке: ≈"+(sim.DailyCost+(scope==0?3:5))+"/день.\nРекомендация: ПК — около 10 мон., телефон — около 5. Оставь запас на счета.",small);
            if(Button(292,566,510,46,"Начать · "+cost+" мон.",S.money>=cost&&S.arrears==0&&S.contractRemaining==0&&S.patchRemaining==0))
            {
                if(sim.StartProject(projectTitle,genre,theme,scope,platform,priceIndex,audience,marketing,sequel))
                { modal=""; sequel=-1; Save(false); Toast("Проект запущен. План выпуска зафиксирован."); }
            }
            if(Button(816,566,172,46,"Назад"))modal="new";
        }
        void DrawResult(){ DrawReleaseReport(); }
        void DrawHistory()
        {
            ModalHeader("Каталог студии", "Нажми «Отчёт»: продажи, расходы и причины результата каждого релиза.");
            historyScroll=GUI.BeginScrollView(new Rect(292,210,696,338),historyScroll,new Rect(0,0,670,Math.Max(330,S.releases.Count*94)));
            if(S.releases.Count==0)Label(0,20,650,60,"Пока нет выпущенных игр.",heading);
            for(int row=0;row<S.releases.Count;row++)
            {
                int index=S.releases.Count-1-row;ReleaseData r=S.releases[index];float y=row*94;
                Label(0,y,480,28,r.title+" · "+r.rating.ToString("F1"),heading);
                Label(0,y+35,495,44,r.legacy?"Архив v1 · Получено "+r.paidRevenue:r.units+" копий · Доход "+r.paidRevenue+" · Прибыль "+(r.paidRevenue-r.cost),small);
                if(Button(516,y+4,138,42,"Отчёт")){selectedRelease=index;reportTab=0;modal="report";}
            }
            GUI.EndScrollView();CloseButton();
        }
        void DrawReleaseReport()
        {
            if(selectedRelease<0||selectedRelease>=S.releases.Count){modal="history";return;}
            ReleaseData r=S.releases[selectedRelease];
            ModalHeader(r.title,r.legacy?"Архив прежней экономики":Market.Platforms[r.platform]+" · "+Catalog.Genres[r.genre]+" / "+Catalog.Themes[r.theme]+" · Цена "+r.price+" мон.");
            if(r.legacy){Label(292,240,680,150,"Оценка: "+r.rating.ToString("F1")+"\nПолучено: "+r.paidRevenue+" / "+r.totalRevenue+" мон.\nДля старого релиза подробные данные не собирались.",heading);CloseButton();return;}
            string[] tabs={"Обзор","Продажи","Разбор"};
            for(int i=0;i<3;i++)if(Button(292+i*236,200,224,36,tabs[i],true,reportTab==i))reportTab=i;
            if(reportTab==0)
            {
                string[] names={"Пиксель","Игромир","Геймпад","Инди-вестник"};
                for(int i=0;i<4;i++)
                {Frame(new Rect(292+i*176,259,164,88),Paper);Label(304+i*176,270,144,24,names[i],small);Label(304+i*176,302,144,38,r.scores[i]+" / 10",heading);}
                Label(292,365,696,55,r.review,heading);
                Label(292,424,340,90,"Копии: "+r.units+"\nВозвраты: "+r.refunds+"\nПоклонники: "+(r.fanChange>=0?"+":"")+r.fanChange);
                Label(650,424,338,90,"Бюджет: "+r.cost+"\nДоход: "+r.paidRevenue+"\nПрибыль: "+(r.paidRevenue-r.cost));
                if(Button(292,528,696,34,r.patched?"Патч выпущен":r.patchStarted?"Патч в разработке":"Выпустить патч · 40 мон., 15 энергии, 24 часа",!r.patchStarted && r.age<Market.WeekHours*(Market.Weeks-1) && S.money>=40 && S.arrears==0 && S.energy>=15 && S.contractRemaining==0 && S.patchRemaining==0))
                    if(sim.StartPatch(selectedRelease)){Save(false);modal="";Toast("Патч в работе. Своя разработка приостановлена на сутки.");}
            }
            else if(reportTab==1)
            {
                int max=1;foreach(SalesWeek w in r.weeks)max=Math.Max(max,w.paid?w.units:0);
                for(int i=0;i<r.weeks.Length;i++)
                {
                    SalesWeek w=r.weeks[i];float h=w.paid?50f*w.units/max:3;
                    Box(new Rect(304+i*114,300-h,80,h),w.paid?mint:Hex("C2BDA3"));
                }
                string[] columns={"Неделя","Копии","Возвраты","Комиссия","Доход"};
                for(int col=0;col<5;col++)Label(300+col*137,314,132,26,columns[col],small);
                for(int i=0;i<r.weeks.Length;i++)
                {
                    SalesWeek w=r.weeks[i];float y=346+i*28;
                    Label(300,y,85,25,(i+1).ToString(),small);
                    if(w.paid)
                    {
                        int[] cells={w.units,w.refunds,w.fee,w.net};
                        for(int col=0;col<4;col++)Label(437+col*137,y,132,25,cells[col].ToString(),small);
                    }
                    else Label(437,y,550,25,"Ожидание · через "+(((i+1)*Market.WeekHours-r.age+23)/24)+" дн.",small);
                }
                Label(292,524,696,32,"Выручка "+r.gross+" − возвраты "+r.refundMoney+" − комиссия "+r.fee+" = "+r.paidRevenue,small);
            }
            else
            {
                Label(292,260,696,156,r.analysis,body);
                float[] weights=Market.Weights[r.genre];
                Label(292,432,696,100,"Важность для жанра: код "+(int)(weights[0]*100)+"%, графика "+(int)(weights[1]*100)+"%, полировка "+(int)(weights[2]*100)+"%.\nМаркетинг: "+Market.Marketing[r.marketing]+". Продолжение: "+(r.sequel>=0?"да":"нет")+".\nПрибыль проекта = доход магазина − бюджет разработки. Общие расходы студии — в финансах.",small);
            }
            if(Button(292,574,696,42,"К каталогу"))modal="history";
        }
        void DrawFinance()
        {
            ModalHeader("Финансы", "Доход от игр после возвратов и комиссии. Долг не исчезает при нулевом балансе.");
            Label(292,221,696,100,"На счету: "+S.money+" мон.\nНеоплаченные счета: "+S.arrears+" мон.\nТекущее содержание: "+sim.DailyCost+" мон./день",heading);
            Label(292,352,696,110,"С начала новой экономики:\nПродажи: "+S.lifetimeSales+" · Заказы: "+S.lifetimeContracts+"\nРасходы: "+S.lifetimeCosts+" · Результат: "+(S.lifetimeSales+S.lifetimeContracts-S.lifetimeCosts));
            Label(292,490,696,60,"При долге разработка и покупки приостановлены. Возьми заказ по телефону: выплата сначала погасит счета.",small);CloseButton();
        }
        void DrawMarket()
        {
            ModalHeader("Рынок", "Прогноз спроса, а не гарантия продаж. Тренды меняются раз в 28 дней.");
            Label(292,222,696,100,"В тренде: "+Catalog.Genres[Market.TrendGenre(S.hour)]+" / "+Catalog.Themes[Market.TrendTheme(S.hour)]+"\nКонкурирующих релизов на неделе: "+Market.Competitors(S.hour)+"\nСледующая смена тренда через "+((672-S.hour%672+23)/24)+" дн.",heading);
            Label(292,350,696,180,"Стратегия: важнее код, опытная аудитория, сильнее на ПК.\nПриключение: важнее графика, доступнее казуальной аудитории.\nГоловоломка: важнее полировка, хорошо подходит телефону.\n\nВысокая цена сокращает число покупателей. Повторы одной темы и жанра в последних трёх релизах снижают спрос. Продолжение игры с оценкой от 6 получает бонус охвата.");CloseButton();
        }
        void DrawResearch()
        {
            ModalHeader("Исследования", "Опыт: "+S.experience+" · Каждое исследование: 120 опыта и 180 монет.");
            for(int i=0;i<3;i++)
            {
                float y=230+i*100;Label(292,y,460,30,Market.ResearchNames[i],heading);Label(292,y+37,460,40,Market.ResearchEffects[i],small);
                if(Button(780,y,208,55,S.research[i]>0?"Изучено":"Исследовать",S.research[i]==0&&S.experience>=120&&S.money>=180&&S.arrears==0))if(sim.Research(i)){Save(false);Toast("Исследование завершено.");}
            }
            CloseButton();
        }
    }
}
