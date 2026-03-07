## Disclaimer
This plugin is only for those who already have a working bot. I won't fix or write your bot; I can stop offering this plugin at any time without warning.

THE PLUGIN IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


## Prerequisite
- A working bot!
- EXtensions
  - The project is called Default for me. Adjust the 3rdparty.json and VS Project accordingly

## Setup
- Clone repo to 3rdparty dir
- Copy archnemesis_recipies.json to DPB/GGPK folder
- Adjust the AddBeforeTaskName variable for your setup
- Call ShouldPickup(string) in your itemfilter and handle it
`ShouldPickup(item.Components.ArchnemesisModComponent.ModWrapper.DisplayName);`

## Configuration options
- Adjust the TargetRecipe to your needs
- Adjust the OptionScore(string) method to your needs
- Check the settings in the bot's ui
