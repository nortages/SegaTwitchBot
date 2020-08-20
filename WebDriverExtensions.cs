using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace NortagesTwitchBot
{
    public static class WebDriverExtensions
    {
        public static void OpenNewTab(this IWebDriver webDriver)
        {
            webDriver.ExecuteJavaScript("window.open();");
            webDriver.SwitchTo().Window(webDriver.WindowHandles.Last());
        }

        public static void CloseCurrentTab(this IWebDriver webDriver)
        {
            webDriver.ExecuteJavaScript("window.close();");
            webDriver.SwitchTo().Window(webDriver.WindowHandles.Last());
        }

        public static IWebElement FindElement(this ISearchContext context, By by, uint timeout, bool displayed = false)
        {
            var wait = new DefaultWait<ISearchContext>(context)
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException));

            try
            {
                return wait.Until(ctx =>
                {
                    var elem = ctx.FindElements(by).FirstOrDefault();
                    if (elem != null)
                    {
                        if (displayed && !elem.Displayed)
                            return null;
                    }                    
                    return elem;
                });
            }
            catch (WebDriverTimeoutException)
            {
                throw new NoSuchElementException();
            }
        }
    }
}
